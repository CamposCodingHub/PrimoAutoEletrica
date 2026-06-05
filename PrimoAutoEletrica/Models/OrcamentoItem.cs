using System;

namespace PrimoAutoEletrica.Models
{
    public class OrcamentoItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrcamentoId { get; set; }
        public Guid? ProdutoId { get; set; }
        public string Tipo { get; set; } = "Produto";
        
        public string ProdutoNome { get; set; } = string.Empty;
        public string ProdutoCodigo { get; set; } = string.Empty;
        public string ProdutoCategoria { get; set; } = string.Empty;
        public string ProdutoMarca { get; set; } = string.Empty;
        public string ProdutoAplicacao { get; set; } = string.Empty;
        
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal PrecoCusto { get; set; }
        public decimal Desconto { get; set; }
        public decimal Subtotal { get; set; }
        public decimal LucroEstimado { get; set; }
        public decimal MargemLucro { get; set; }
        
        public int EstoqueDisponivel { get; set; }
        public string Observacoes { get; set; } = string.Empty;
        public bool UsaEstoque => string.Equals(Tipo, "Produto", StringComparison.OrdinalIgnoreCase);
        public string TipoDescricao => UsaEstoque ? "Produto" : "Mao de obra";
        
        // Propriedades de navegação
        public Produto? Produto { get; set; }
    }
}
