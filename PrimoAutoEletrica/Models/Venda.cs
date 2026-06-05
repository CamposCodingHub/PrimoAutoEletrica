using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public class Venda
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Data { get; set; } = DateTime.Now;
        public Cliente? Cliente { get; set; }
        public List<ItemVenda> Itens { get; set; } = new();
        public decimal Total { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public decimal Desconto { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Status { get; set; } = "Concluida";
        public Guid? CaixaSessaoId { get; set; }
        public DateTime? DataCancelamento { get; set; }
        public string CanceladoPor { get; set; } = string.Empty;
        public string MotivoCancelamento { get; set; } = string.Empty;
    }
}
