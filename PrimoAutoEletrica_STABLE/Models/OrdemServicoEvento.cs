using System;

namespace PrimoAutoEletrica.Models
{
    public class OrdemServicoEvento
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrdemServicoId { get; set; }
        public DateTime DataEvento { get; set; } = DateTime.Now;
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
    }
}
