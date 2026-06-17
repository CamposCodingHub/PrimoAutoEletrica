using System;

namespace PrimoAutoEletrica.Models
{
    public class CatalogoImportacaoPreviewItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CodigoFabricante { get; set; } = string.Empty;
        public string CodigoNormalizado { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Subcategoria { get; set; } = string.Empty;
        public string Aplicacao { get; set; } = string.Empty;
        public string VeiculoAplicacao { get; set; } = string.Empty;
        public string Voltagem { get; set; } = string.Empty;
        public string Amperagem { get; set; } = string.Empty;
        public string QuantidadeTerminais { get; set; } = string.Empty;
        public string TipoProduto { get; set; } = string.Empty;
        public string PaginaCatalogo { get; set; } = string.Empty;
        public string FonteCatalogo { get; set; } = string.Empty;
        public string StatusRevisao { get; set; } = "Novo";
        public bool DuplicadoProvavel { get; set; }
        public string MensagemValidacao { get; set; } = string.Empty;
        public string ConteudoOriginal { get; set; } = string.Empty;
        public string ArquivoOrigem { get; set; } = string.Empty;
        public string ObservacoesTecnicas { get; set; } = string.Empty;
        public string Linha { get; set; } = string.Empty;

        public string NomeExibicao => string.IsNullOrWhiteSpace(Nome) ? CodigoFabricante : Nome;
    }
}
