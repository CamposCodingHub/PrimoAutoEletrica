using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public enum StatusTransferenciaEstoque
    {
        Solicitada = 1,
        EmTransito = 2,
        Recebida = 3,
        Cancelada = 4
    }

    public sealed class TransferenciaEstoque
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string NumeroTransferencia { get; set; } = string.Empty; // Ex: TRF-2026-0001
        
        public Guid FilialOrigemId { get; set; }
        public string FilialOrigemNome { get; set; } = string.Empty;
        
        public Guid FilialDestinoId { get; set; }
        public string FilialDestinoNome { get; set; } = string.Empty;
        
        public StatusTransferenciaEstoque Status { get; set; } = StatusTransferenciaEstoque.Solicitada;
        
        public DateTime DataSolicitacao { get; set; } = DateTime.Now;
        public DateTime? DataEnvio { get; set; }
        public DateTime? DataRecebimento { get; set; }
        
        public string ResponsavelSolicitacao { get; set; } = string.Empty;
        public string? ResponsavelEnvio { get; set; }
        public string? ResponsavelRecebimento { get; set; }
        
        public string Observacoes { get; set; } = string.Empty;
        public decimal ValorTotalEstimado { get; set; }
        
        public List<TransferenciaEstoqueItem> Itens { get; set; } = new();

        public string StatusDescricao => Status switch
        {
            StatusTransferenciaEstoque.Solicitada => "Solicitada",
            StatusTransferenciaEstoque.EmTransito => "Em Trânsito",
            StatusTransferenciaEstoque.Recebida => "Recebida e Conferida",
            StatusTransferenciaEstoque.Cancelada => "Cancelada",
            _ => "Indefinido"
        };
    }

    public sealed class TransferenciaEstoqueItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TransferenciaId { get; set; }
        public int ProdutoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int QuantidadeEnviada { get; set; }
        public int QuantidadeRecebida { get; set; }
        public decimal ValorUnitario { get; set; }
        
        public decimal ValorTotal => QuantidadeEnviada * ValorUnitario;
    }
}
