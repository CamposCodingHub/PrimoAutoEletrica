using System;

namespace PrimoAutoEletrica.Models
{
    public class CatalogoImportacao
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime DataImportacao { get; set; } = DateTime.Now;
        public string ArquivoNome { get; set; } = string.Empty;
        public string ArquivoCaminho { get; set; } = string.Empty;
        public string TipoArquivo { get; set; } = string.Empty;
        public string FonteCatalogo { get; set; } = string.Empty;
        public string MarcaDetectada { get; set; } = string.Empty;
        public int TotalLidos { get; set; }
        public int TotalImportados { get; set; }
        public int TotalDuplicados { get; set; }
        public int TotalComErro { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Resumo { get; set; } = string.Empty;
        public string LogDetalhado { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
    }
}
