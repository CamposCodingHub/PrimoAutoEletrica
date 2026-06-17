using System;

namespace PrimoAutoEletrica.Models
{
    public sealed class ProdutoFornecedor
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProdutoId { get; set; }
        public Guid FornecedorId { get; set; }
        public string CodigoProduto { get; set; } = string.Empty;
        public string NomeProduto { get; set; } = string.Empty;
        public string CategoriaProduto { get; set; } = string.Empty;
        public string CodigoFornecedor { get; set; } = string.Empty;
        public decimal PrecoUltimaCompra { get; set; }
        public decimal QuantidadeUltimaCompra { get; set; }
        public int PrazoEntregaDias { get; set; }
        public int QuantidadeCompras { get; set; }
        public decimal ValorCompras { get; set; }
        public DateTime? DataUltimaCompra { get; set; }
        public string ChaveUltimaNFe { get; set; } = string.Empty;
        public string NumeroUltimaNFe { get; set; } = string.Empty;
        public bool Ativo { get; set; } = true;
        public string Origem { get; set; } = "Estoque";
        public string Observacoes { get; set; } = string.Empty;
        public DateTime DataCadastro { get; set; } = DateTime.Now;
        public DateTime? DataUltimaAtualizacao { get; set; }
    }
}
