namespace PrimoAutoEletrica.Models
{
    public class CatalogoPecaFiltro
    {
        public string Termo { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string StatusRevisao { get; set; } = string.Empty;
        public bool SomentePendentes { get; set; }
        public bool IncluirInativos { get; set; }
    }
}
