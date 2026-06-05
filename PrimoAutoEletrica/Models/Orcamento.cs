using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public class Orcamento
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid? ClienteId { get; set; }
        public Guid? VendedorId { get; set; }
        
        public string Numero { get; set; } = string.Empty;
        public string Status { get; set; } = "Em Aberto"; // Em Aberto, Aguardando Cliente, Aprovado, Recusado, Vencido, Convertido em Venda, Cancelado, Em Negociação
        
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime? DataValidade { get; set; }
        public DateTime? DataAprovacao { get; set; }
        public DateTime? DataConversaoVenda { get; set; }
        public DateTime? DataConversaoOrdemServico { get; set; }
        public Guid? OrdemServicoId { get; set; }
        
        public decimal Subtotal { get; set; }
        public decimal Desconto { get; set; }
        public decimal Acrescimo { get; set; }
        public decimal Total { get; set; }
        public decimal MargemLucro { get; set; }
        public decimal LucroEstimado { get; set; }
        public decimal ComissaoVendedor { get; set; }
        public decimal ImpostosEstimados { get; set; }
        
        public string Observacoes { get; set; } = string.Empty;
        public string CondicoesPagamento { get; set; } = string.Empty;
        public string PrazoEntrega { get; set; } = string.Empty;
        
        public List<OrcamentoItem> Itens { get; set; } = new();
        
        // Propriedades de navegação
        public Cliente? Cliente { get; set; }
    }
}
