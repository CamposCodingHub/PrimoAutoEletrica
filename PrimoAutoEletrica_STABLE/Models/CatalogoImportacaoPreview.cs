using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public class CatalogoImportacaoPreview
    {
        public string ArquivoOrigem { get; set; } = string.Empty;
        public string TipoArquivo { get; set; } = string.Empty;
        public string FonteDetectada { get; set; } = string.Empty;
        public string MarcaDetectada { get; set; } = string.Empty;
        public int TotalItens { get; set; }
        public int TotalDuplicadosProvaveis { get; set; }
        public int TotalJaExistentes { get; set; }
        public int TotalIncompletos { get; set; }
        public int TotalComErro { get; set; }
        public int TotalValidos { get; set; }
        public int TotalSemNomeReal { get; set; }
        public int TotalSemDescricao { get; set; }
        public string AlertaQualidade { get; set; } = string.Empty;
        public bool ImportacaoArriscada { get; set; }
        public List<CatalogoImportacaoPreviewItem> Itens { get; set; } = new();
        public List<CatalogoImportacaoErro> Erros { get; set; } = new();
    }
}
