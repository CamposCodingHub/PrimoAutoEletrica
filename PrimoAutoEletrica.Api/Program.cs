using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://localhost:5001", "http://localhost:5000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictiveCors", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey))
{
    if (builder.Environment.IsDevelopment())
        signingKey = "DEV_ONLY_PRIMOX_JWT_SIGNING_KEY_MIN_32_CHARS!!";
    else
        throw new InvalidOperationException(
            "Jwt:SigningKey e obrigatorio em Production (>=32 caracteres). API nao sobe sem autenticacao.");
}

if (!builder.Environment.IsDevelopment()
    && (signingKey.Contains("CHANGE_ME", StringComparison.Ordinal)
        || signingKey.StartsWith("DEV_ONLY_", StringComparison.Ordinal)))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey placeholder (CHANGE_ME/DEV_ONLY) nao permitido fora de Development.");
}

if (signingKey.Length < 32)
    throw new InvalidOperationException("Jwt:SigningKey deve ter pelo menos 32 caracteres.");

var issuer = jwtSection["Issuer"] ?? "Primox.Api";
var audience = jwtSection["Audience"] ?? "Primox.Clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    void AddPerm(string code) =>
        options.AddPolicy(code, p => p.RequireAuthenticatedUser().RequireClaim("perm", code));

    AddPerm("ORCAMENTO_LER");
    AddPerm("ORCAMENTO_CRIAR");
    AddPerm("ORCAMENTO_EDITAR");
    AddPerm("ORCAMENTO_EXCLUIR");
    AddPerm("ESTOQUE_LER");
    AddPerm("ESTOQUE_AJUSTAR");
    AddPerm("FINANCEIRO_LER");
    AddPerm("FINANCEIRO_EDITAR");
});

builder.Services.AddSingleton<LoggerService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<OrcamentoDatabaseService>();
builder.Services.AddSingleton<EstoqueOperationalService>();
builder.Services.AddSingleton<FinanceiroDatabaseService>();
builder.Services.AddSingleton(sp =>
{
    var db = sp.GetRequiredService<DatabaseService>();
    var logger = sp.GetRequiredService<LoggerService>();
    return new ProdutoRepository(db.GetConnection, logger);
});

var app = builder.Build();
var apiLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Primox.Api");

IResult SafeProblem(Exception ex, string publicMessage)
{
    apiLogger.LogError(ex, "{PublicMessage}", publicMessage);
    // Nunca devolver a mensagem da exception ao consumidor (vazamento de detalhes internos).
    return Results.Json(
        new { detail = publicMessage, status = StatusCodes.Status500InternalServerError, title = "An error occurred while processing your request." },
        statusCode: StatusCodes.Status500InternalServerError,
        contentType: "application/problem+json");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        apiLogger.LogError(ex, "Unhandled exception captured by global API guard");
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        var path = context.Request.Path.Value ?? string.Empty;
        var detail = path.Contains("/api/financeiro/resumo", StringComparison.OrdinalIgnoreCase)
            ? "Erro ao obter resumo financeiro."
            : "Ocorreu um erro interno no servidor.";
        await context.Response.WriteAsJsonAsync(new
        {
            detail = detail,
            status = 500,
            title = "An error occurred while processing your request."
        });
    }
});

app.UseCors("RestrictiveCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("api");

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0",
    Auth = "JwtBearer",
    Message = "API PrimoAutoEletrica funcionando (health publico)"
}))
.WithName("HealthCheck")
.AllowAnonymous()
.RequireRateLimiting("api");

// Token local: client_id/client_secret em Jwt:Clients (nao e OAuth completo; e auth efetiva).
app.MapPost("/api/auth/token", (TokenRequest body, IConfiguration config, IHostEnvironment env) =>
{
    if (body is null || string.IsNullOrWhiteSpace(body.ClientId) || string.IsNullOrWhiteSpace(body.ClientSecret))
        return Results.BadRequest(new { error = "client_id e client_secret obrigatorios" });

    var clients = config.GetSection("Jwt:Clients").GetChildren();
    var match = clients.FirstOrDefault(c =>
        string.Equals(c["ClientId"], body.ClientId, StringComparison.Ordinal) &&
        string.Equals(c["ClientSecret"], body.ClientSecret, StringComparison.Ordinal));

    if (match is null && env.IsDevelopment()
        && body.ClientId == "dev" && body.ClientSecret == "dev")
    {
        // Credencial de desenvolvimento apenas.
    }
    else if (match is null)
    {
        return Results.Unauthorized();
    }

    var perms = match?["Permissions"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ?? new[]
        {
            "ORCAMENTO_LER", "ORCAMENTO_CRIAR", "ORCAMENTO_EDITAR", "ORCAMENTO_EXCLUIR",
            "ESTOQUE_LER", "ESTOQUE_AJUSTAR", "FINANCEIRO_LER", "FINANCEIRO_EDITAR"
        };

    var key = config["Jwt:SigningKey"];
    if (string.IsNullOrWhiteSpace(key) && env.IsDevelopment())
        key = "DEV_ONLY_PRIMOX_JWT_SIGNING_KEY_MIN_32_CHARS!!";

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, body.ClientId),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        new("client_id", body.ClientId)
    };
    foreach (var p in perms)
        claims.Add(new Claim("perm", p));

    var creds = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"] ?? "Primox.Api",
        audience: config["Jwt:Audience"] ?? "Primox.Clients",
        claims: claims,
        notBefore: DateTime.UtcNow,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { access_token = jwt, token_type = "Bearer", expires_in = 28800 });
})
.WithName("EmitirToken")
.AllowAnonymous()
.RequireRateLimiting("api");

app.MapGet("/api/orcamentos", (OrcamentoDatabaseService orcamentoService) =>
{
    try { return Results.Ok(orcamentoService.ObterTodosOrcamentos()); }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao listar orcamentos."); }
})
.WithName("ListarOrcamentos")
.RequireAuthorization("ORCAMENTO_LER")
.RequireRateLimiting("api");

app.MapGet("/api/orcamentos/{id}", (Guid id, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        var orcamento = orcamentoService.ObterOrcamentoPorId(id);
        return orcamento == null
            ? Results.NotFound(new { error = "Orcamento nao encontrado" })
            : Results.Ok(orcamento);
    }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao obter orcamento."); }
})
.WithName("ObterOrcamentoPorId")
.RequireAuthorization("ORCAMENTO_LER")
.RequireRateLimiting("api");

app.MapPost("/api/orcamentos", (Orcamento orcamento, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        if (orcamento == null) return Results.BadRequest(new { error = "Dados invalidos" });
        if (orcamento.Id == Guid.Empty) orcamento.Id = Guid.NewGuid();
        orcamentoService.AdicionarOrcamento(orcamento);
        return Results.Created($"/api/orcamentos/{orcamento.Id}", orcamento);
    }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao criar orcamento."); }
})
.WithName("CriarOrcamento")
.RequireAuthorization("ORCAMENTO_CRIAR")
.RequireRateLimiting("api");

app.MapPut("/api/orcamentos/{id}", (Guid id, Orcamento orcamento, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        if (orcamento == null) return Results.BadRequest(new { error = "Dados invalidos" });
        orcamento.Id = id;
        orcamentoService.AtualizarOrcamento(orcamento);
        return Results.Ok(orcamento);
    }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao atualizar orcamento."); }
})
.WithName("AtualizarOrcamento")
.RequireAuthorization("ORCAMENTO_EDITAR")
.RequireRateLimiting("api");

app.MapDelete("/api/orcamentos/{id}", (Guid id, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        orcamentoService.ExcluirOrcamento(id);
        return Results.NoContent();
    }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao excluir orcamento."); }
})
.WithName("ExcluirOrcamento")
.RequireAuthorization("ORCAMENTO_EXCLUIR")
.RequireRateLimiting("api");

app.MapGet("/api/orcamentos/{id}/itens", (Guid id, OrcamentoDatabaseService orcamentoService) =>
{
    try { return Results.Ok(orcamentoService.ObterItensDoOrcamento(id)); }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao obter itens do orcamento."); }
})
.WithName("ObterItensOrcamento")
.RequireAuthorization("ORCAMENTO_LER")
.RequireRateLimiting("api");

app.MapGet("/api/estoque/produtos", (ProdutoRepository produtoRepository) =>
{
    try { return Results.Ok(produtoRepository.ObterTodos()); }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao listar produtos."); }
})
.WithName("ListarProdutosEstoque")
.RequireAuthorization("ESTOQUE_LER")
.RequireRateLimiting("api");

app.MapGet("/api/estoque/produtos/{id}", (Guid id, ProdutoRepository produtoRepository) =>
{
    try
    {
        var produto = produtoRepository.ObterPorId(id);
        return produto == null
            ? Results.NotFound(new { error = "Produto nao encontrado" })
            : Results.Ok(produto);
    }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao obter produto."); }
})
.WithName("ObterProdutoPorId")
.RequireAuthorization("ESTOQUE_LER")
.RequireRateLimiting("api");

app.MapGet("/api/financeiro/resumo/{inicio}/{fim}", (DateTime inicio, DateTime fim, FinanceiroDatabaseService financeiroService) =>
{
    try { return Results.Ok(financeiroService.ObterResumoFinanceiro(inicio, fim)); }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao obter resumo financeiro."); }
})
.WithName("ObterResumoFinanceiroPorPeriodo")
.RequireAuthorization("FINANCEIRO_LER")
.RequireRateLimiting("api");

app.MapPost("/api/financeiro/orcamento", (Orcamento orcamento, FinanceiroDatabaseService financeiroService) =>
{
    try
    {
        if (orcamento == null) return Results.BadRequest(new { error = "Dados invalidos" });
        financeiroService.RegistrarReceitaOrcamento(orcamento);
        return Results.Ok(new { mensagem = "Receita do orcamento registrada com sucesso" });
    }
    catch (Exception ex) { return SafeProblem(ex, "Erro ao registrar receita do orcamento."); }
})
.WithName("RegistrarReceitaOrcamento")
.RequireAuthorization("FINANCEIRO_EDITAR")
.RequireRateLimiting("api");

app.Run();

public sealed record TokenRequest(string ClientId, string ClientSecret);

// Exposto para testes de integracao / WebApplicationFactory.
public partial class Program { }
