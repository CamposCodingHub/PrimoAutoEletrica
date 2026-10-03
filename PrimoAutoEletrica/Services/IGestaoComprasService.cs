using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services
{
    public sealed class ResumoNecessidadeCompras
    {
        public int TotalItensEmFalta { get; set; }
        public int RupturasCriticas { get; set; }
        public int ItensAbaixoMinimo { get; set; }
        public decimal CustoTotalEstimado { get; set; }
        public int FornecedoresImpactados { get; set; }
    }

    public interface IGestaoComprasService
    {
        Task<List<ItemFaltaEstoque>> ObterNecessidadesReposicaoAsync(
            string? termoBusca = null,
            NivelUrgenciaFalta? filtroUrgencia = null,
            Guid? fornecedorId = null,
            string? curvaAbc = null);

        Task<ResumoNecessidadeCompras> ObterResumoNecessidadesAsync();

        Task<PedidoCompra> GerarRascunhoPedidoAsync(Guid fornecedorId, IEnumerable<ItemFaltaEstoque> itens);

        Task SalvarPedidoCompraAsync(PedidoCompra pedido);

        Task<List<PedidoCompra>> ListarPedidosCompraAsync(StatusPedidoCompra? status = null, Guid? fornecedorId = null);

        Task<PedidoCompra?> ObterPedidoCompraPorIdAsync(Guid pedidoId);

        Task AtualizarStatusPedidoAsync(Guid pedidoId, StatusPedidoCompra novoStatus, string? chaveNFe = null);

        string FormatarMensagemCotacaoWhatsApp(PedidoCompra pedido, string nomeOficina = "PRIMOX Auto Elétrica");

        Task<bool> BaixarPedidoComNFeAsync(string chaveNfe, Guid fornecedorId, Dictionary<string, int> itensEntregues);
    }
}
