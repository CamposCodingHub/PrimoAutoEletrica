using System;

namespace PrimoAutoEletrica.Models
{
    public class HistoricoServico
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTime DataServico { get; set; }

        public string Descricao { get; set; } = string.Empty;

        public decimal Valor { get; set; }

        public string TecnicoResponsavel { get; set; } = string.Empty;

        public string Observacoes { get; set; } = string.Empty;
    }
}