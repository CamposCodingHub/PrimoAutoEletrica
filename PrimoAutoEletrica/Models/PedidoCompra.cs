using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public enum StatusPedidoCompra
    {
        Rascunho = 1,
        CotacaoEnviada = 2,
        AprovadoAguardandoEntrega = 3,
        RecebidoParcial = 4,
        RecebidoTotal = 5,
        Cancelado = 6
    }

    public sealed class PedidoCompra
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Numero { get; set; } = string.Empty;
        public Guid FornecedorId { get; set; }
        public string FornecedorNome { get; set; } = string.Empty;
        public string FornecedorCNPJ { get; set; } = string.Empty;
        public string FornecedorTelefone { get; set; } = string.Empty;
        public string FornecedorEmail { get; set; } = string.Empty;
        public StatusPedidoCompra Status { get; set; } = StatusPedidoCompra.Rascunho;
        public decimal ValorTotal { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime? DataEnvioCotacao { get; set; }
        public DateTime? PrevisaoEntrega { get; set; }
        public DateTime? DataRecebimento { get; set; }
        public string? ChaveNFeVinculada { get; set; }
        public string? NumeroNFe { get; set; }
        public string FormaPagamento { get; set; } = string.Empty;
        public string CondicaoPagamento { get; set; } = string.Empty;
        public string Observacoes { get; set; } = string.Empty;
        public string CriadoPor { get; set; } = "Sistema";
        public List<PedidoCompraItem> Itens { get; set; } = new();

        public string StatusDescricao => Status switch
        {
            StatusPedidoCompra.Rascunho => "Rascunho",
            StatusPedidoCompra.CotacaoEnviada => "Cotação Enviada",
            StatusPedidoCompra.AprovadoAguardandoEntrega => "Aguardando Entrega",
            StatusPedidoCompra.RecebidoParcial => "Recebido Parcial",
            StatusPedidoCompra.RecebidoTotal => "Recebido Total",
            StatusPedidoCompra.Cancelado => "Cancelado",
            _ => "Desconhecido"
        };
    }

    public sealed class PedidoCompraItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PedidoCompraId { get; set; }
        public Guid ProdutoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int QuantidadePedida { get; set; }
        public int QuantidadeRecebida { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal Subtotal => QuantidadePedida * ValorUnitario;
        public int PendenteRecebimento => Math.Max(0, QuantidadePedida - QuantidadeRecebida);
    }
}
