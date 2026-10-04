using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PRIMOX.Application.DTOs;
using PRIMOX.Application.Interfaces;
using PRIMOX.Application.UseCases.OrdensServico;
using PRIMOX.Domain.Interfaces;
using PRIMOX.Infrastructure.Persistence;
using PRIMOX.Infrastructure.Time;
using PrimoAutoEletrica.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "https://localhost:5001", "http://localhost:5000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictiveCors", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
});

// Injeção de Dependência da Arquitetura PRIMOX 3.0 (Domain / Application / Infrastructure)
var dbPath = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=primoauto.db";
builder.Services.AddSingleton<ITimeProvider>(DefaultTimeProvider.Instance);
builder.Services.AddSingleton<IOrdemServicoRepository>(sp => new SqliteOrdemServicoRepository(dbPath));

// Casos de Uso
builder.Services.AddScoped<AbrirOrdemServicoUseCase>();
builder.Services.AddScoped<ObterOrdemServicoUseCase>();
builder.Services.AddScoped<AdicionarItemPecaUseCase>();
builder.Services.AddScoped<AdicionarItemServicoUseCase>();
builder.Services.AddScoped<AlterarStatusOrdemServicoUseCase>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PRIMOX Workshop API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("RestrictiveCors");

// Health Check
app.MapGet("/api/health", (ITimeProvider timeProvider) => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = timeProvider.GetUtcNow(),
    Version = "3.0.0-fase1",
    Plataforma = "PRIMOX Clean Architecture Headless (Cross-Platform)",
    Message = "API PRIMOX desacoplada de WPF e operando em .NET 10"
}))
.WithName("HealthCheck")
.WithTags("Monitoramento");

// ==========================================
// VERTICAL SLICE DA ORDEM DE SERVIÇO (API V1)
// ==========================================

// 1. Abertura de Ordem de Serviço
app.MapPost("/api/v1/ordens-servico", async (
    AbrirOrdemServicoCommand command,
    AbrirOrdemServicoUseCase useCase) =>
{
    var resultado = await useCase.ExecutarAsync(command);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.MensagemErro });
    }

    return Results.Created($"/api/v1/ordens-servico/{resultado.OrdemServicoId}", resultado);
})
.WithName("AbrirOrdemServico")
.WithTags("Ordens de Serviço");

// 2. Consulta de Ordem de Serviço por ID
app.MapGet("/api/v1/ordens-servico/{id:guid}", async (
    Guid id,
    ObterOrdemServicoUseCase useCase) =>
{
    var resultado = await useCase.ExecutarAsync(new ObterOrdemServicoQuery(id));
    if (!resultado.Sucesso)
    {
        return Results.NotFound(new { erro = resultado.MensagemErro });
    }

    return Results.Ok(resultado.Dados);
})
.WithName("ObterOrdemServicoPorId")
.WithTags("Ordens de Serviço");

// 3. Adição de Peça à Ordem de Serviço
app.MapPost("/api/v1/ordens-servico/{id:guid}/itens-peca", async (
    Guid id,
    AdicionarItemPecaRequest request,
    AdicionarItemPecaUseCase useCase) =>
{
    var command = new AdicionarItemPecaCommand(
        OrdemServicoId: id,
        Descricao: request.Descricao,
        Quantidade: request.Quantidade,
        ValorUnitario: request.ValorUnitario,
        Codigo: request.Codigo ?? "",
        CustoUnitario: request.CustoUnitario,
        ProdutoId: request.ProdutoId,
        Moeda: request.Moeda ?? "BRL");

    var resultado = await useCase.ExecutarAsync(command);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.MensagemErro });
    }

    return Results.Created($"/api/v1/ordens-servico/{id}/itens-peca/{resultado.ItemId}", resultado);
})
.WithName("AdicionarItemPeca")
.WithTags("Ordens de Serviço");

// 4. Adição de Serviço / Mão de Obra à Ordem de Serviço
app.MapPost("/api/v1/ordens-servico/{id:guid}/itens-servico", async (
    Guid id,
    AdicionarItemServicoRequest request,
    AdicionarItemServicoUseCase useCase) =>
{
    var command = new AdicionarItemServicoCommand(
        OrdemServicoId: id,
        Descricao: request.Descricao,
        QuantidadeHoras: request.QuantidadeHoras,
        ValorHora: request.ValorHora,
        TecnicoResponsavel: request.TecnicoResponsavel,
        ServicoId: request.ServicoId,
        Moeda: request.Moeda ?? "BRL");

    var resultado = await useCase.ExecutarAsync(command);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.MensagemErro });
    }

    return Results.Created($"/api/v1/ordens-servico/{id}/itens-servico/{resultado.ItemId}", resultado);
})
.WithName("AdicionarItemServico")
.WithTags("Ordens de Serviço");

// 5. Alteração de Status da Ordem de Serviço
app.MapPost("/api/v1/ordens-servico/{id:guid}/status", async (
    Guid id,
    AlterarStatusRequest request,
    AlterarStatusOrdemServicoUseCase useCase) =>
{
    var command = new AlterarStatusOrdemServicoCommand(
        OrdemServicoId: id,
        NovoStatus: request.NovoStatus,
        Motivo: request.Motivo,
        Responsavel: request.Responsavel);

    var resultado = await useCase.ExecutarAsync(command);
    if (!resultado.Sucesso)
    {
        return Results.BadRequest(new { erro = resultado.MensagemErro });
    }

    return Results.Ok(resultado);
})
.WithName("AlterarStatusOrdemServico")
.WithTags("Ordens de Serviço");

app.Run();

// Contratos de Requisição HTTP da API
public record AdicionarItemPecaRequest(
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario,
    string? Codigo,
    decimal? CustoUnitario,
    Guid? ProdutoId,
    string? Moeda);

public record AdicionarItemServicoRequest(
    string Descricao,
    decimal QuantidadeHoras,
    decimal ValorHora,
    string? TecnicoResponsavel,
    Guid? ServicoId,
    string? Moeda);

public record AlterarStatusRequest(
    string NovoStatus,
    string Motivo,
    string Responsavel);
