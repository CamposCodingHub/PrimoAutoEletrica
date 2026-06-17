using System;

namespace PrimoAutoEletrica.Models
{
    public class OrdemServicoItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrdemServicoId { get; set; }
        public Guid? ProdutoId { get; set; }

        public string Tipo { get; set; } = "Servico";
        public string Descricao { get; set; } = string.Empty;
        public decimal Quantidade { get; set; } = 1;
        public decimal ValorUnitario { get; set; }
        public decimal CustoUnitario { get; set; }
        public string Observacoes { get; set; } = string.Empty;
        public int OrdemExibicao { get; set; }
        public bool EstoqueMovimentado { get; set; }

        public decimal Total => Quantidade * ValorUnitario;
        public decimal CustoTotal => Quantidade * CustoUnitario;
    }
}
