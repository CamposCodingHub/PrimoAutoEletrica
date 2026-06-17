using System;

namespace PrimoAutoEletrica.Models
{
    public class ItemVenda
    {
        public Produto? Produto { get; set; }
        public Guid? ProdutoId { get; set; }
        public string Tipo { get; set; } = "Produto";
        public string Descricao { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal CustoUnitario { get; set; }
        public decimal Desconto { get; set; }
        public bool UsaEstoque => string.Equals(Tipo, "Produto", StringComparison.OrdinalIgnoreCase);
        public string NomeExibicao => string.IsNullOrWhiteSpace(Descricao)
            ? Produto?.Nome ?? "Item"
            : Descricao;
        public decimal Subtotal => (PrecoUnitario * Quantidade) - Desconto;
    }
}
