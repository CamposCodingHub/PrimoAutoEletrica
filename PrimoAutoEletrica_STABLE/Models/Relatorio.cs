using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public class Relatorio
    {
        public Guid Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public DateTime DataGeracao { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string UsuarioGerou { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string FiltrosAplicados { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public int TotalRegistros { get; set; }
        public string CaminhoArquivo { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;
    }

    public class DadoFinanceiro
    {
        public Guid Id { get; set; }
        public DateTime Data { get; set; }
        public string Tipo { get; set; } = string.Empty; // Receita, Despesa, Lucro
        public string Categoria { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
    }

    public class DemonstrativoResultadoFinanceiro
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public decimal ReceitasConfirmadas { get; set; }
        public decimal DespesasConfirmadas { get; set; }
        public decimal ContasReceberPendentes { get; set; }
        public decimal ContasPagarPendentes { get; set; }
        public decimal InadimplenciaEmAberto { get; set; }
        public decimal ResultadoOperacional => ReceitasConfirmadas - DespesasConfirmadas;
        public decimal ResultadoProjetado => ResultadoOperacional + ContasReceberPendentes - ContasPagarPendentes;
    }

    public class ResumoExecutivoFinanceiro
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public decimal FaturamentoPeriodo { get; set; }
        public decimal LucroOrdensServico { get; set; }
        public decimal LucroProdutos { get; set; }
        public decimal LucroServicos { get; set; }
        public decimal DespesasFixas { get; set; }
        public decimal DespesasVariaveis { get; set; }
        public decimal RecebimentosPorForma { get; set; }
        public decimal SaldoCaixaOperadores { get; set; }
        public decimal Inadimplencia { get; set; }
        public decimal ResultadoGeral => FaturamentoPeriodo - DespesasFixas - DespesasVariaveis;
    }

    public class LucroFinanceiroItem
    {
        public string Tipo { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string Detalhe { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal Receita { get; set; }
        public decimal Custo { get; set; }
        public decimal LucroBruto { get; set; }
        public decimal MargemPercentual { get; set; }
        public DateTime DataReferencia { get; set; }
        public string Origem { get; set; } = string.Empty;
    }

    public class DespesaTipoResumo
    {
        public string Tipo { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int Quantidade { get; set; }
        public decimal Percentual { get; set; }
        public string Descricao { get; set; } = string.Empty;
    }

    public class RecebimentoFormaPagamentoResumo
    {
        public string FormaPagamento { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public int Quantidade { get; set; }
        public decimal Percentual { get; set; }
    }

    public class CaixaOperadorResumo
    {
        public string Operador { get; set; } = string.Empty;
        public decimal Entradas { get; set; }
        public decimal Saidas { get; set; }
        public decimal Saldo { get; set; }
        public decimal TotalVendas { get; set; }
        public int QuantidadeVendas { get; set; }
        public int Movimentacoes { get; set; }
        public decimal TicketMedio => QuantidadeVendas > 0 ? TotalVendas / QuantidadeVendas : 0m;
    }

    public class FaturamentoPeriodoFinanceiro
    {
        public string Periodo { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        public decimal Receitas { get; set; }
        public decimal Despesas { get; set; }
        public decimal Resultado => Receitas - Despesas;
    }

    public class DadoVenda
    {
        public Guid Id { get; set; }
        public DateTime Data { get; set; }
        public Guid ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public Guid VendedorId { get; set; }
        public string VendedorNome { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public decimal Desconto { get; set; }
        public decimal Lucro { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ItensQuantidade { get; set; }
    }

    public class DadoMargemProduto
    {
        public Guid ProdutoId { get; set; }
        public string ProdutoNome { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public int QuantidadeVendida { get; set; }
        public decimal ReceitaTotal { get; set; }
        public decimal CustoTotal { get; set; }
        public decimal LucroBruto { get; set; }
        public decimal MargemPercentual { get; set; }
    }

    public class DadoVendaPeriodo
    {
        public string Periodo { get; set; } = string.Empty;
        public DateTime? Data { get; set; }
        public int? Hora { get; set; }
        public int QuantidadeVendas { get; set; }
        public int ItensVendidos { get; set; }
        public decimal Faturamento { get; set; }
        public decimal TicketMedio { get; set; }
    }

    public class DadoInadimplencia
    {
        public int Id { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        public int DiasAtraso { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FormaPagamento { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
        public string ReferenciaExterna { get; set; } = string.Empty;
        public string FaixaAtraso => DiasAtraso switch
        {
            <= 0 => "No prazo",
            <= 7 => "1-7 dias",
            <= 30 => "8-30 dias",
            <= 60 => "31-60 dias",
            _ => "+60 dias"
        };
    }

    public class DadoConciliacaoFinanceira
    {
        public string FormaPagamento { get; set; } = string.Empty;
        public decimal TotalVendas { get; set; }
        public decimal EntradasFinanceiras { get; set; }
        public decimal Diferenca => TotalVendas - EntradasFinanceiras;
        public int QuantidadeVendas { get; set; }
        public int QuantidadeMovimentacoes { get; set; }
        public string Status => Math.Abs(Diferenca) <= 0.01m
            ? "Conciliado"
            : Diferenca > 0
                ? "Venda sem entrada"
                : "Entrada sem venda";
    }

    public class DadoOrdemServicoRelatorio
    {
        public Guid Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string ClienteNome { get; set; } = string.Empty;
        public string TecnicoNome { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DataAbertura { get; set; }
        public DateTime? DataConclusao { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal LucroBruto { get; set; }
        public int TotalItens { get; set; }
        public int TempoRealMinutos { get; set; }
    }

    public class DadoOrdemServicoTecnico
    {
        public string TecnicoNome { get; set; } = string.Empty;
        public int OrdensAbertas { get; set; }
        public int OrdensFinalizadas { get; set; }
        public int TotalOrdens => OrdensAbertas + OrdensFinalizadas;
        public decimal ValorTotal { get; set; }
        public decimal LucroBruto { get; set; }
        public int TempoRealMinutos { get; set; }
        public decimal TicketMedio => TotalOrdens > 0 ? ValorTotal / TotalOrdens : 0m;
    }

    public class DadoServicoRelatorio
    {
        public string Servico { get; set; } = string.Empty;
        public string Origem { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal ReceitaTotal { get; set; }
        public decimal CustoTotal { get; set; }
        public decimal LucroBruto => ReceitaTotal - CustoTotal;
        public decimal MargemPercentual => ReceitaTotal > 0 ? LucroBruto / ReceitaTotal * 100m : 0m;
        public DateTime UltimaExecucao { get; set; }
    }

    public class DadoEstoque
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public string ProdutoNome { get; set; } = string.Empty;
        public string ProdutoCodigo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public int QuantidadeAtual { get; set; }
        public int QuantidadeMinima { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        public string CurvaAbc { get; set; } = "Sem valor";
        public decimal ParticipacaoEstoquePercentual { get; set; }
        public decimal ParticipacaoAcumuladaPercentual { get; set; }
        public int GiroMensal { get; set; }
        public int DiasSemMovimentacao { get; set; }
        public DateTime UltimaMovimentacao { get; set; }
    }

    public class DadoCliente
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Ativo, VIP, Inadimplente, Inativo
        public decimal TotalCompras { get; set; }
        public int NumeroCompras { get; set; }
        public decimal TicketMedio { get; set; }
        public DateTime UltimaCompra { get; set; }
        public decimal LimiteCredito { get; set; }
        public decimal Inadimplencia { get; set; }
    }

    public class DadoOrcamento
    {
        public Guid Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime DataValidade { get; set; }
        public Guid ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public Guid VendedorId { get; set; }
        public string VendedorNome { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public string Status { get; set; } = string.Empty; // Em Aberto, Aprovado, Recusado, Convertido em Venda
        public DateTime? DataAprovacao { get; set; }
        public DateTime? DataConversaoVenda { get; set; }
        public int ItensQuantidade { get; set; }
    }

    public class DadoCaixa
    {
        public Guid Id { get; set; }
        public DateTime Data { get; set; }
        public string Tipo { get; set; } = string.Empty; // Abertura, Fechamento, Sangria, Suprimento
        public decimal ValorInicial { get; set; }
        public decimal ValorFinal { get; set; }
        public decimal Sangrias { get; set; }
        public decimal Suprimentos { get; set; }
        public decimal Diferenca { get; set; }
        public string Operador { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;
    }

    public class DadoMeta
    {
        public Guid Id { get; set; }
        public string Tipo { get; set; } = string.Empty; // Vendas, Faturamento, Lucro
        public string Periodo { get; set; } = string.Empty; // Diário, Semanal, Mensal, Anual
        public decimal MetaValor { get; set; }
        public decimal ValorAtual { get; set; }
        public decimal PercentualAtingido { get; set; }
        public string Responsavel { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Status { get; set; } = string.Empty; // Em Andamento, Atingida, Não Atingida
    }

    public class DadoAlerta
    {
        public Guid Id { get; set; }
        public string Tipo { get; set; } = string.Empty; // Queda Faturamento, Aumento Despesas, Inadimplência, Estoque Baixo
        public string Mensagem { get; set; } = string.Empty;
        public string Severidade { get; set; } = string.Empty; // Baixa, Média, Alta, Crítica
        public DateTime DataGeracao { get; set; }
        public bool Lido { get; set; }
        public string Origem { get; set; } = string.Empty;
    }

    public class DadoAuditoria
    {
        public Guid Id { get; set; }
        public DateTime DataHora { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public string Tabela { get; set; } = string.Empty;
        public Guid RegistroId { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Severidade { get; set; } = string.Empty;
        public bool Sucesso { get; set; }
        public string Perfil { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string Maquina { get; set; } = string.Empty;
        public string ValorAnterior { get; set; } = string.Empty;
        public string ValorNovo { get; set; } = string.Empty;
        public string IP { get; set; } = string.Empty;
        public string StatusOperacional => Sucesso ? "Sucesso" : "Falha";
    }

    public class DadoTimeline
    {
        public Guid Id { get; set; }
        public DateTime DataHora { get; set; }
        public string TipoEvento { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public decimal? Valor { get; set; }
        public string Categoria { get; set; } = string.Empty;
    }

    public class DadoComparativo
    {
        public Guid Id { get; set; }
        public string PeriodoAtual { get; set; } = string.Empty;
        public string PeriodoAnterior { get; set; } = string.Empty;
        public decimal ValorAtual { get; set; }
        public decimal ValorAnterior { get; set; }
        public decimal Variacao { get; set; }
        public decimal PercentualVariacao { get; set; }
        public string Tendencia { get; set; } = string.Empty; // Crescimento, Queda, Estável
    }
    public class AuditoriaOperacionalFiltro
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Severidade { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string TermoLivre { get; set; } = string.Empty;
        public int PaginaAtual { get; set; } = 1;
        public int ItensPorPagina { get; set; } = 100;
    }

    public class QueryPageResult<T>
    {
        public List<T> Itens { get; set; } = new();
        public int PaginaAtual { get; set; }
        public int ItensPorPagina { get; set; }
        public int TotalItens { get; set; }
        public int TotalPaginas { get; set; }
        public bool TemPaginaAnterior => PaginaAtual > 1;
        public bool TemProximaPagina => PaginaAtual < TotalPaginas;
    }
}
