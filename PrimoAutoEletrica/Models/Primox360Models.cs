using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    /// <summary>
    /// Agregados Cliente/Veículo/OS 360 — apenas métricas com origem comprovável por ID.
    /// Valores financeiros de ContasReceber sem ClienteId / Origem+ReferenciaExterna = N/A (nome não entra).
    /// </summary>
    public sealed class Cliente360Snapshot
    {
        public Guid ClienteId { get; init; }
        public string Nome { get; init; } = string.Empty;
        public int VeiculosCount { get; init; }
        public int OsCount { get; init; }
        public int VisitasCount { get; init; }
        public decimal ReceitaOsTotal { get; init; }
        public decimal ReceitaVendasTotal { get; init; }
        public decimal ReceitaTotal => ReceitaOsTotal + ReceitaVendasTotal;
        public decimal ReceitaOs12Meses { get; init; }
        public decimal ReceitaVendas12Meses { get; init; }
        public decimal Receita12Meses => ReceitaOs12Meses + ReceitaVendas12Meses;
        public decimal? TicketMedioOs { get; init; }
        public int OrcamentosCount { get; init; }
        public int OrcamentosAprovados { get; init; }
        public int OrcamentosRecusados { get; init; }
        public decimal ValorPerdidoOrcamentos { get; init; }
        public DateTime? UltimaVisita { get; init; }
        public int? DiasDesdeUltimaVisita { get; init; }
        public decimal? TotalGastoCadastro { get; init; }
        /// <summary>ContasReceber ligadas por ClienteId e/ou Origem+ReferenciaExterna (OS/Orçamento). Sem match por nome.</summary>
        public decimal DividaVinculadaPorId { get; init; }
        public int ContasReceberVinculadasPendentes { get; init; }
        /// <summary>True quando existe ContasReceber sem vínculo ID — KPI de dívida total NÃO é confiável.</summary>
        public bool DividaTotalConfiavel => true; // ClienteId + vinculo Origem/Referencia
        public string DividaTotalDisplay { get; init; } = "N/A / NÃO DISPONÍVEL";
        public string FonteDivida { get; init; } = "ClienteId e/ou Origem+ReferenciaExterna. TEXT_MATCH por nome nao entra no KPI.";
        public IReadOnlyList<Cliente360TimelineItem> Timeline { get; init; } = Array.Empty<Cliente360TimelineItem>();
        public IReadOnlyList<Guid> VeiculoIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<Guid> OrdemServicoIds { get; init; } = Array.Empty<Guid>();
    }

    public sealed class Cliente360TimelineItem
    {
        public DateTime Data { get; init; }
        public string Tipo { get; init; } = string.Empty;
        public string Titulo { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public Guid? ReferenciaId { get; init; }
    }

    public sealed class Veiculo360Snapshot
    {
        public Guid VeiculoId { get; init; }
        public Guid? ClienteId { get; init; }
        public string Placa { get; init; } = string.Empty;
        public int Quilometragem { get; init; }
        public int OsCountPorVeiculoId { get; init; }
        public int OsCountIncluindoPlacaFraca { get; init; }
        public decimal ReceitaAcumuladaPorVeiculoId { get; init; }
        public DateTime? UltimoServico { get; init; }
        public int? DiasDesdeUltimoServico { get; init; }
        public int OrcamentosPorVeiculoId { get; init; }
        public string ProblemaRecorrenteCadastro { get; init; } = string.Empty;
        public bool UsaFallbackPlaca { get; init; }
        public string NotaIntegridade { get; init; } = string.Empty;
        public IReadOnlyList<Guid> OrdemServicoIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<DiagnosticoTecnico> Diagnosticos { get; init; } = Array.Empty<DiagnosticoTecnico>();
        public int DiagnosticosCount => Diagnosticos.Count;
    }

    public sealed class OrdemServico360Snapshot
    {
        public Guid OrdemServicoId { get; init; }
        public string Numero { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public Guid ClienteId { get; init; }
        public Guid? VeiculoId { get; init; }
        public Guid? OrcamentoId { get; init; }
        public string ClienteLink { get; init; } = "CONNECTED";
        public string VeiculoLink { get; init; } = "MISSING";
        public string OrcamentoLink { get; init; } = "MISSING";
        public string FinanceiroLink { get; init; } = "MISSING";
        public string FiscalLink { get; init; } = "MISSING";
        public string PosVendaLink { get; init; } = "MISSING";
        public string DiagnosticoLink { get; init; } = "MISSING";
        public IReadOnlyList<DiagnosticoTecnico> Diagnosticos { get; init; } = Array.Empty<DiagnosticoTecnico>();
        public decimal TotalItens { get; init; }
        public int ItensServico { get; init; }
        public int ItensPeca { get; init; }
        public bool TemFotos { get; init; }
        public bool TemAssinatura { get; init; }
        public string HubResumo { get; init; } = string.Empty;
    }

    public sealed class ContaReceberVinculo
    {
        public int Id { get; init; }
        public string ClienteNomeSnapshot { get; init; } = string.Empty;
        public string Descricao { get; init; } = string.Empty;
        public decimal Valor { get; init; }
        public DateTime DataVencimento { get; init; }
        public DateTime? DataPagamento { get; init; }
        public string Status { get; init; } = string.Empty;
        public string FormaPagamento { get; init; } = string.Empty;
        public string Origem { get; init; } = string.Empty;
        public string ReferenciaExterna { get; init; } = string.Empty;
        public bool Pago { get; init; }
    }

    public sealed class Produto360Snapshot
    {
        public Guid ProdutoId { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nome { get; init; } = string.Empty;
        public int EstoqueAtual { get; init; }
        public int EstoqueMinimo { get; init; }
        public int EstoqueDisponivel { get; init; }
        public string Fornecedor { get; init; } = string.Empty;
        public decimal Custo { get; init; }
        public decimal Preco { get; init; }
        public decimal MargemPercentual { get; init; }
        public int OsComUsoCount { get; init; }
        public decimal QuantidadeUsadaEmOs { get; init; }
        public decimal ValorUsadoEmOs { get; init; }
        public IReadOnlyList<Guid> OrdemServicoIds { get; init; } = Array.Empty<Guid>();
        public IReadOnlyList<string> OrdemServicoResumos { get; init; } = Array.Empty<string>();
        public string HubResumo { get; init; } = string.Empty;
        public bool EstoqueCritico { get; init; }
    }
}
