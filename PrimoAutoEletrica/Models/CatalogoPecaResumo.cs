namespace PrimoAutoEletrica.Models
{
    public class CatalogoPecaResumo
    {
        public int TotalItens { get; set; }
        public int PendentesRevisao { get; set; }
        public int ConvertidosEstoque { get; set; }
        public int DuplicadosProvaveis { get; set; }
        public int Incompletos { get; set; }
        public int Ignorados { get; set; }
    }
}
