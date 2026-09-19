using System;

namespace PrimoAutoEletrica.Models
{
    public class CatalogoPecaFiltro
    {
        public string Termo { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string StatusRevisao { get; set; } = string.Empty;
        public bool SomentePendentes { get; set; }
        public string Aplicacao { get; set; } = string.Empty;
        public string Equivalentes { get; set; } = string.Empty;
        public string ModeloVeiculo { get; set; } = string.Empty;
        public string Ano { get; set; } = string.Empty;
        public string Motor { get; set; } = string.Empty;
        public string MarcaVeiculo { get; set; } = string.Empty;
        public Guid? CatalogoVeiculoId { get; set; }
        public bool SomenteAutoEletrica { get; set; } = true;
        public bool IncluirInativos { get; set; }
    }
}
