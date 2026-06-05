using System;

namespace PrimoAutoEletrica.Models
{
    public class ServicoCliente
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        public decimal Valor { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Observacoes { get; set; }
    }
}
