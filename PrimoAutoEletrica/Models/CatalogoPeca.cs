using System;

namespace PrimoAutoEletrica.Models
{
    public class CatalogoPeca
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CodigoFabricante { get; set; } = string.Empty;
        public string CodigoNormalizado { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Subcategoria { get; set; } = string.Empty;
        public string Linha { get; set; } = string.Empty;
        public string Aplicacao { get; set; } = string.Empty;
        public string VeiculoAplicacao { get; set; } = string.Empty;
        public int? AnoInicial { get; set; }
        public int? AnoFinal { get; set; }
        public string Voltagem { get; set; } = string.Empty;
        public string Amperagem { get; set; } = string.Empty;
        public string QuantidadeTerminais { get; set; } = string.Empty;
        public string TipoProduto { get; set; } = string.Empty;
        public string PaginaCatalogo { get; set; } = string.Empty;
        public string FonteCatalogo { get; set; } = string.Empty;
        public string ArquivoOrigem { get; set; } = string.Empty;
        public string ObservacoesTecnicas { get; set; } = string.Empty;
        public string ImagemUrl { get; set; } = string.Empty;
        public string ImagemLocal { get; set; } = string.Empty;
        public string StatusRevisao { get; set; } = "Pendente";
        public Guid? ProdutoEstoqueId { get; set; }
        public DateTime DataImportacao { get; set; } = DateTime.Now;
        public DateTime DataAtualizacao { get; set; } = DateTime.Now;
        public bool Ativo { get; set; } = true;

        public bool EstaVinculadoAoEstoque => ProdutoEstoqueId.HasValue && ProdutoEstoqueId.Value != Guid.Empty;
        public string NomeExibicao => string.IsNullOrWhiteSpace(Nome) ? CodigoFabricante : Nome;
    }
}
