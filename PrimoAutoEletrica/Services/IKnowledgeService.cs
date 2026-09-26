using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public interface IKnowledgeService
    {
        Task<IReadOnlyList<TechnicalKnowledgeEntry>> ListarArtigosAsync(string? busca = null, string? sistema = null, string? tensao = null, KnowledgeStatus? status = null, CancellationToken ct = default);
        Task<TechnicalKnowledgeEntry?> ObterArtigoPorIdAsync(Guid knowledgeId, CancellationToken ct = default);
        Task<TechnicalKnowledgeEntry?> ObterArtigoPorCodigoAsync(string codigo, CancellationToken ct = default);
        Task<bool> SalvarArtigoAsync(TechnicalKnowledgeEntry entry, CancellationToken ct = default);
        Task<bool> ExcluirArtigoAsync(Guid knowledgeId, CancellationToken ct = default);

        Task<IReadOnlyList<DiagnosticCase>> ListarCasosAsync(string? busca = null, string? sistema = null, Guid? veiculoId = null, Guid? osId = null, CancellationToken ct = default);
        Task<DiagnosticCase?> ObterCasoPorIdAsync(Guid caseId, CancellationToken ct = default);
        Task<bool> SalvarCasoAsync(DiagnosticCase caso, CancellationToken ct = default);
        Task<bool> ExcluirCasoAsync(Guid caseId, CancellationToken ct = default);
        Task<IReadOnlyList<DiagnosticCase>> ListarCasosPorVeiculoAsync(Guid veiculoId, CancellationToken ct = default);

        Task<DiagnosticCase> CriarCasoAPartirDeOSAsync(Guid osId, string sistema, string causaConfirmada, string solucao, string? medicoes = null, string? pecasUtilizadas = null, string? dtcCodes = null, CancellationToken ct = default);
    }
}
