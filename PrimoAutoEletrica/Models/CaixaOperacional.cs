using System;

namespace PrimoAutoEletrica.Models
{
    public class CaixaSessaoOperacional
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NumeroCaixa { get; set; } = "01";
        public DateTime DataAbertura { get; set; } = DateTime.Now;
        public DateTime? DataFechamento { get; set; }
        public int? OperadorId { get; set; }
        public string OperadorNome { get; set; } = string.Empty;
        public string PerfilOperador { get; set; } = string.Empty;
        public decimal ValorAbertura { get; set; }
        public decimal ValorEsperado { get; set; }
        public decimal? ValorInformadoFechamento { get; set; }
        public decimal TotalVendas { get; set; }
        public decimal TotalSangrias { get; set; }
        public decimal TotalSuprimentos { get; set; }
        public int QuantidadeVendas { get; set; }
        public string Status { get; set; } = "Fechado";
        public string Observacoes { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime? DataUltimaMovimentacao { get; set; }

        public decimal DiferencaFechamento =>
            ValorInformadoFechamento.HasValue
                ? ValorInformadoFechamento.Value - ValorEsperado
                : 0m;

        public bool Aberto => string.Equals(Status, "Aberto", StringComparison.OrdinalIgnoreCase);
    }

    public class CaixaMovimentacaoOperacional
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CaixaSessaoId { get; set; }
        public DateTime Data { get; set; } = DateTime.Now;
        public string Tipo { get; set; } = string.Empty;
        public decimal ValorMovimento { get; set; }
        public decimal ValorInicial { get; set; }
        public decimal ValorFinal { get; set; }
        public decimal Sangrias { get; set; }
        public decimal Suprimentos { get; set; }
        public decimal Diferenca { get; set; }
        public string Operador { get; set; } = string.Empty;
        public string FormaPagamento { get; set; } = string.Empty;
        public string ReferenciaId { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;
    }
}
