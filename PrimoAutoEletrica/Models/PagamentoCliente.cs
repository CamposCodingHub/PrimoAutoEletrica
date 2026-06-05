using System;

namespace PrimoAutoEletrica.Models
{
    public class PagamentoCliente
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public decimal Valor { get; set; }
        public string Metodo { get; set; } = string.Empty;
        public DateTime Vencimento { get; set; }
        public bool Pago { get; set; }
        public DateTime? DataPagamento { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
