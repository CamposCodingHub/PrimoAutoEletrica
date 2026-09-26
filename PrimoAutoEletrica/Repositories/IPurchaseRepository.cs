using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Repositories
{
    public interface IPurchaseRepository
    {
        Task<IReadOnlyList<PurchaseRequest>> ObterTodosAsync(string? busca = null, PurchasePriority? prioridade = null, PurchaseRequestStatus? status = null, int? solicitanteId = null, CancellationToken ct = default);
        Task<PurchaseRequest?> ObterPorIdAsync(Guid requestId, CancellationToken ct = default);
        Task<PurchaseRequest?> ObterPorNumeroAsync(string numero, CancellationToken ct = default);
        Task<bool> InserirAsync(PurchaseRequest request, CancellationToken ct = default);
        Task<bool> AtualizarAsync(PurchaseRequest request, CancellationToken ct = default);
        Task<bool> AtualizarStatusAsync(Guid requestId, PurchaseRequestStatus status, int usuarioId, string? motivo = null, CancellationToken ct = default);
        Task<bool> ExcluirAsync(Guid requestId, CancellationToken ct = default);

        Task<IReadOnlyList<PurchaseRequestItem>> ObterItensAsync(Guid requestId, CancellationToken ct = default);
        Task<bool> InserirItemAsync(PurchaseRequestItem item, CancellationToken ct = default);
        Task<bool> AtualizarItemAsync(PurchaseRequestItem item, CancellationToken ct = default);
        Task<bool> ExcluirItemAsync(Guid itemId, CancellationToken ct = default);

        Task<IReadOnlyList<Produto>> ObterProdutosAbaixoEstoqueMinimoAsync(CancellationToken ct = default);
    }
}
