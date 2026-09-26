using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public interface IPurchaseService
    {
        Task<IReadOnlyList<PurchaseRequest>> ListarRequisicoesAsync(string? busca = null, PurchasePriority? prioridade = null, PurchaseRequestStatus? status = null, int? solicitanteId = null, CancellationToken ct = default);
        Task<PurchaseRequest?> ObterPorIdAsync(Guid requestId, CancellationToken ct = default);
        Task<PurchaseRequest?> ObterPorNumeroAsync(string numero, CancellationToken ct = default);

        Task<IReadOnlyList<Produto>> ObterSugestoesEstoqueMinimoAsync(CancellationToken ct = default);

        Task<PurchaseRequest> CriarRequisicaoAsync(int usuarioId, string usuarioNome, PurchasePriority prioridade, PurchaseReason motivo, string? observacoes = null, List<PurchaseRequestItem>? itensIniciais = null, CancellationToken ct = default);
        Task<bool> AdicionarItemAsync(Guid requestId, Guid produtoId, decimal quantidade, PurchasePriority prioridade, string? motivo = null, CancellationToken ct = default);
        Task<bool> RemoverItemAsync(Guid itemId, CancellationToken ct = default);

        Task<bool> AprovarRequisicaoAsync(Guid requestId, int usuarioId, string usuarioNome, CancellationToken ct = default);
        Task<bool> CancelarRequisicaoAsync(Guid requestId, int usuarioId, string motivo, CancellationToken ct = default);
        Task<bool> FormalizarPedidoAsync(Guid requestId, Guid? fornecedorId, string fornecedorNome, decimal custoTotalEstimado, CancellationToken ct = default);
        Task<bool> ReceberMercadoriaAsync(Guid requestId, int usuarioId, string? documentoFiscal, decimal? custoEfetivoTotal = null, Dictionary<Guid, decimal>? quantidadesRecebidas = null, CancellationToken ct = default);
    }
}
