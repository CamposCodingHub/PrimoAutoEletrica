using System;

namespace PrimoAutoEletrica.Models
{
    public class CatalogoImportacaoErro
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid? ImportacaoId { get; set; }
        public string LinhaOrigem { get; set; } = string.Empty;
        public string CodigoDetectado { get; set; } = string.Empty;
        public string MensagemErro { get; set; } = string.Empty;
        public string ConteudoOriginal { get; set; } = string.Empty;
        public DateTime DataErro { get; set; } = DateTime.Now;
    }
}
