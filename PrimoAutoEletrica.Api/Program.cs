using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();


// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register application services
builder.Services.AddSingleton<LoggerService>();
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<OrcamentoDatabaseService>();
builder.Services.AddSingleton<EstoqueOperationalService>();
builder.Services.AddSingleton<FinanceiroDatabaseService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Timestamp = DateTime.Now,
        Version = "1.0.0",
        Message = "API PrimoAutoEletrica funcionando corretamente"
    });
})
.WithName("HealthCheck")
.WithOpenApi();

// Orcamentos API endpoints
app.MapGet("/api/orcamentos", (OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        var orcamentos = orcamentoService.ObterTodosOrcamentos();
        return Results.Ok(orcamentos);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao listar orçamentos: {ex.Message}", statusCode: 500);
    }
})
.WithName("ListarOrcamentos")
.WithOpenApi();

app.MapGet("/api/orcamentos/{id}", (Guid id, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        var orcamento = orcamentoService.ObterOrcamentoPorId(id);
        if (orcamento == null)
        {
            return Results.NotFound($"Orçamento com ID {id} não encontrado");
        }
        return Results.Ok(orcamento);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao obter orçamento: {ex.Message}", statusCode: 500);
    }
})
.WithName("ObterOrcamentoPorId")
.WithOpenApi();

app.MapPost("/api/orcamentos", (Orcamento orcamento, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        if (orcamento == null)
        {
            return Results.BadRequest("Dados do orçamento inválidos");
        }

        if (orcamento.Id == Guid.Empty)
        {
            orcamento.Id = Guid.NewGuid();
        }

        orcamentoService.AdicionarOrcamento(orcamento);
        return Results.Created($"/api/orcamentos/{orcamento.Id}", orcamento);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao criar orçamento: {ex.Message}", statusCode: 500);
    }
})
.WithName("CriarOrcamento")
.WithOpenApi();

app.MapPut("/api/orcamentos/{id}", (Guid id, Orcamento orcamento, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        if (orcamento == null)
        {
            return Results.BadRequest("Dados do orçamento inválidos");
        }

        orcamento.Id = id;
        orcamentoService.AtualizarOrcamento(orcamento);
        return Results.Ok(orcamento);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao atualizar orçamento: {ex.Message}", statusCode: 500);
    }
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
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao excluir orçamento: {ex.Message}", statusCode: 500);
    }
})
.WithName("ExcluirOrcamento")
.WithOpenApi();

app.MapGet("/api/orcamentos/{id}/itens", (Guid id, OrcamentoDatabaseService orcamentoService) =>
{
    try
    {
        var itens = orcamentoService.ObterItensDoOrcamento(id);
        return Results.Ok(itens);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao obter itens do orçamento: {ex.Message}", statusCode: 500);
    }
})
.WithName("ObterItensOrcamento")
.WithOpenApi();

// Estoque API endpoints
app.MapGet("/api/estoque/produtos", (EstoqueOperationalService estoqueService) =>
{
    try
    {
        var produtos = App.Repositories.Produtos.ObterTodos();
        return Results.Ok(produtos);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao listar produtos: {ex.Message}", statusCode: 500);
    }
})
.WithName("ListarProdutosEstoque")
.WithOpenApi();

app.MapGet("/api/estoque/produtos/{id}", (Guid id, EstoqueOperationalService estoqueService) =>
{
    try
    {
        var produto = App.Repositories.Produtos.ObterPorId(id);
        if (produto == null)
        {
            return Results.NotFound($"Produto com ID {id} não encontrado");
        }
        return Results.Ok(produto);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao obter produto: {ex.Message}", statusCode: 500);
    }
})
.WithName("ObterProdutoPorId")
.WithOpenApi();

// Financeiro API endpoints
app.MapGet("/api/financeiro/resumo/{inicio}/{fim}", (DateTime inicio, DateTime fim, FinanceiroDatabaseService financeiroService) =>
{
    try
    {
        var resumo = financeiroService.ObterResumoFinanceiro(inicio, fim);
        return Results.Ok(resumo);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao obter resumo financeiro: {ex.Message}", statusCode: 500);
    }
})
.WithName("ObterResumoFinanceiroPorPeriodo")
.WithOpenApi();

app.MapPost("/api/financeiro/orcamento", (Orcamento orcamento, FinanceiroDatabaseService financeiroService) =>
{
    try
    {
        if (orcamento == null)
        {
            return Results.BadRequest("Dados do orçamento inválidos");
        }

        financeiroService.RegistrarReceitaOrcamento(orcamento);
        return Results.Ok(new { mensagem = "Receita do orçamento registrada com sucesso" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao registrar receita do orçamento: {ex.Message}", statusCode: 500);
    }
})
.WithName("RegistrarReceitaOrcamento")
.WithOpenApi();

app.Run();
