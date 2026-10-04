using System;
using System.Collections.Generic;

namespace PRIMOX.Application.DTOs
{
    public record ItemPecaDto(
        Guid Id,
        Guid OrdemServicoId,
        string Codigo,
        string Descricao,
        decimal Quantidade,
        decimal ValorUnitario,
        decimal CustoUnitario,
        decimal Subtotal,
        string Moeda,
        Guid? ProdutoId);

    public record ItemServicoDto(
        Guid Id,
        Guid OrdemServicoId,
        string Descricao,
        decimal QuantidadeHoras,
        decimal ValorHora,
        decimal Subtotal,
        string Moeda,
        string? TecnicoResponsavel,
        Guid? ServicoId);

    public record HistoricoStatusDto(
        Guid Id,
        string StatusAnterior,
        string NovoStatus,
        string Motivo,
        string Responsavel,
        DateTimeOffset DataHora);

    public record OrdemServicoDto(
        Guid Id,
        string Numero,
        Guid ClienteId,
        Guid VeiculoId,
        string ClienteNome,
        string VeiculoPlaca,
        string VeiculoModelo,
        string Status,
        string Prioridade,
        string QueixaCliente,
        string DiagnosticoTecnico,
        string ObservacoesInternas,
        DateTimeOffset DataAbertura,
        DateTimeOffset? DataPrevisaoConclusao,
        DateTimeOffset? DataConclusao,
        decimal TotalPecas,
        decimal TotalServicos,
        decimal Desconto,
        decimal TotalBruto,
        decimal TotalLiquido,
        string Moeda,
        Guid? SessaoDiagnosticoId,
        Guid TenantId,
        Guid FilialId,
        IReadOnlyList<ItemPecaDto> ItensPeca,
        IReadOnlyList<ItemServicoDto> ItensServico,
        IReadOnlyList<HistoricoStatusDto> Historico);
}
