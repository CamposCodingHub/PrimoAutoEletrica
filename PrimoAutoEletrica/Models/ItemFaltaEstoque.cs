using System;

namespace PrimoAutoEletrica.Models
{
    public enum NivelUrgenciaFalta
    {
        Critica = 1,   // Saldo real <= 0 e/ou OS ativa aguardando o item
        Alta = 2,      // Saldo real abaixo ou igual ao estoque minimo
        Media = 3,     // Saldo real abaixo do ponto de pedido (ROP)
        Preventiva = 4 // Reposicao de estoque de giro (Curva A/B)
    }

    public sealed class ItemFaltaEstoque
    {
        public Guid ProdutoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Localizacao { get; set; } = string.Empty;

        public int QuantidadeEstoque { get; set; }
        public int QuantidadeMinima { get; set; }
        public int QuantidadeReservadaOS { get; set; }
        public int SaldoRealDisponivel => QuantidadeEstoque - QuantidadeReservadaOS;

        public decimal ConsumoMedioDiario { get; set; }
        public int LeadTimeDias { get; set; } = 3;
        public int EstoqueSeguranca { get; set; } = 2;
        public int PontoDePedido { get; set; }

        public int QuantidadeSugeridaCompra { get; set; }
        public decimal UltimoCustoCompra { get; set; }
        public decimal PrecoVendaAtual { get; set; }
        public decimal ValorTotalEstimado => QuantidadeSugeridaCompra * UltimoCustoCompra;

        public NivelUrgenciaFalta Urgencia { get; set; } = NivelUrgenciaFalta.Media;
        public string CurvaAbc { get; set; } = "B";

        public Guid? FornecedorPreferencialId { get; set; }
        public string FornecedorPreferencialNome { get; set; } = string.Empty;
        public string FornecedorTelefone { get; set; } = string.Empty;
        public string FornecedorWhatsApp { get; set; } = string.Empty;

        // Propriedade operacional de interface
        public bool Selecionado { get; set; } = false;

        public string UrgenciaDescricao => Urgencia switch
        {
            NivelUrgenciaFalta.Critica => "Crítica (Ruptura)",
            NivelUrgenciaFalta.Alta => "Alta (Abaixo Mínimo)",
            NivelUrgenciaFalta.Media => "Média (Ponto Pedido)",
            NivelUrgenciaFalta.Preventiva => "Preventiva (Giro)",
            _ => "Normal"
        };
    }
}
