using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Repositories
{
    public interface IKnowledgeRepository
    {
        Task<IReadOnlyList<TechnicalKnowledgeEntry>> ObterArtigosAsync(string? busca = null, string? sistema = null, string? tensao = null, KnowledgeStatus? status = null, CancellationToken ct = default);
        Task<TechnicalKnowledgeEntry?> ObterArtigoPorIdAsync(Guid knowledgeId, CancellationToken ct = default);
        Task<TechnicalKnowledgeEntry?> ObterArtigoPorCodigoAsync(string codigo, CancellationToken ct = default);
        Task<bool> InserirArtigoAsync(TechnicalKnowledgeEntry entry, CancellationToken ct = default);
        Task<bool> AtualizarArtigoAsync(TechnicalKnowledgeEntry entry, CancellationToken ct = default);
        Task<bool> ExcluirArtigoAsync(Guid knowledgeId, CancellationToken ct = default);

        Task<IReadOnlyList<DiagnosticCase>> ObterCasosAsync(string? busca = null, string? sistema = null, Guid? veiculoId = null, Guid? osId = null, CancellationToken ct = default);
        Task<DiagnosticCase?> ObterCasoPorIdAsync(Guid caseId, CancellationToken ct = default);
        Task<bool> InserirCasoAsync(DiagnosticCase caso, CancellationToken ct = default);
        Task<bool> AtualizarCasoAsync(DiagnosticCase caso, CancellationToken ct = default);
        Task<bool> ExcluirCasoAsync(Guid caseId, CancellationToken ct = default);
        Task<IReadOnlyList<DiagnosticCase>> ObterCasosPorVeiculoAsync(Guid veiculoId, CancellationToken ct = default);
    }
}
