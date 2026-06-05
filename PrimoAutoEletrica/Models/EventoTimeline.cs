using System;

namespace PrimoAutoEletrica.Models
{
    public class EventoTimeline
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Icone { get; set; } = string.Empty;
    }
}
