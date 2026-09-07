using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://localhost:5001", "http://localhost:5000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictiveCors", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("RestrictiveCors");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.Now,
    Version = "1.0.0",
    Message = "API PrimoAutoEletrica funcionando corretamente"
}))
.WithName("HealthCheck")
.WithOpenApi();

app.MapGet("/api/orcamentos", (OrcamentoDatabaseService orcamentoService) =>
{
    try { return Results.Ok(orcamentoService.ObterTodosOrcamentos()); }
    catch (Exception ex) { return Results.Problem($"Erro ao listar orçamentos: {ex.Message}", statusCode: 500); }
})
.WithName("ListarOrcamentos")
.WithOpenApi();

app.MapGet("/api/orcamentos/{id}", (Guid id, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        var orcamento = orcamentoService.ObterOrcamentoPorId(id);
        return orcamento == null
            ? Results.NotFound($"Orçamento com ID {id} não encontrado")
            : Results.Ok(orcamento);
    }
    catch (Exception ex) { return Results.Problem($"Erro ao obter orçamento: {ex.Message}", statusCode: 500); }
})
.WithName("ObterOrcamentoPorId")
.WithOpenApi();

app.MapPost("/api/orcamentos", (Orcamento orcamento, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        if (orcamento == null) return Results.BadRequest("Dados do orçamento inválidos");
        if (orcamento.Id == Guid.Empty) orcamento.Id = Guid.NewGuid();
        orcamentoService.AdicionarOrcamento(orcamento);
        return Results.Created($"/api/orcamentos/{orcamento.Id}", orcamento);
    }
    catch (Exception ex) { return Results.Problem($"Erro ao criar orçamento: {ex.Message}", statusCode: 500); }
})
.WithName("CriarOrcamento")
.WithOpenApi();

app.MapPut("/api/orcamentos/{id}", (Guid id, Orcamento orcamento, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        if (orcamento == null) return Results.BadRequest("Dados do orçamento inválidos");
        orcamento.Id = id;
        orcamentoService.AtualizarOrcamento(orcamento);
        return Results.Ok(orcamento);
    }
    catch (Exception ex) { return Results.Problem($"Erro ao atualizar orçamento: {ex.Message}", statusCode: 500); }
})
.WithName("AtualizarOrcamento")
.WithOpenApi();

app.MapDelete("/api/orcamentos/{id}", (Guid id, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        orcamentoService.ExcluirOrcamento(id);
        return Results.NoContent();
    }
    catch (Exception ex) { return Results.Problem($"Erro ao excluir orçamento: {ex.Message}", statusCode: 500); }
})
.WithName("ExcluirOrcamento")
.WithOpenApi();

app.MapGet("/api/orcamentos/{id}/itens", (Guid id, OrcamentoDatabaseService orcamentoService) =>
{
    try { return Results.Ok(orcamentoService.ObterItensDoOrcamento(id)); }
    catch (Exception ex) { return Results.Problem($"Erro ao obter itens do orçamento: {ex.Message}", statusCode: 500); }
})
.WithName("ObterItensOrcamento")
.WithOpenApi();

app.MapGet("/api/estoque/produtos", (ProdutoRepository produtoRepository) =>
{
    try { return Results.Ok(produtoRepository.ObterTodos()); }
    catch (Exception ex) { return Results.Problem($"Erro ao listar produtos: {ex.Message}", statusCode: 500); }
})
.WithName("ListarProdutosEstoque")
.WithOpenApi();

app.MapGet("/api/estoque/produtos/{id}", (Guid id, ProdutoRepository produtoRepository) =>
{
    try
    {
        var produto = produtoRepository.ObterPorId(id);
        return produto == null
            ? Results.NotFound($"Produto com ID {id} não encontrado")
            : Results.Ok(produto);
    }
    catch (Exception ex) { return Results.Problem($"Erro ao obter produto: {ex.Message}", statusCode: 500); }
})
.WithName("ObterProdutoPorId")
.WithOpenApi();

app.MapGet("/api/financeiro/resumo/{inicio}/{fim}", (DateTime inicio, DateTime fim, FinanceiroDatabaseService financeiroService) =>
{
    try { return Results.Ok(financeiroService.ObterResumoFinanceiro(inicio, fim)); }
    catch (Exception ex) { return Results.Problem($"Erro ao obter resumo financeiro: {ex.Message}", statusCode: 500); }
})
.WithName("ObterResumoFinanceiroPorPeriodo")
.WithOpenApi();

app.MapPost("/api/financeiro/orcamento", (Orcamento orcamento, FinanceiroDatabaseService financeiroService) =>
{
    try
    {
        if (orcamento == null) return Results.BadRequest("Dados do orçamento inválidos");
        financeiroService.RegistrarReceitaOrcamento(orcamento);
        return Results.Ok(new { mensagem = "Receita do orçamento registrada com sucesso" });
    }
    catch (Exception ex) { return Results.Problem($"Erro ao registrar receita do orçamento: {ex.Message}", statusCode: 500); }
})
.WithName("RegistrarReceitaOrcamento")
.WithOpenApi();

app.Run();
