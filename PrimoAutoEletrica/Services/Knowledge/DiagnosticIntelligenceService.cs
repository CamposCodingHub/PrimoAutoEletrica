using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IDiagnosticIntelligenceService
    {
        Task<DiagnosticIntelligenceResult> AnalyzeAsync(DiagnosticIntelligenceRequest request, CancellationToken ct = default);
    }

    public sealed class DiagnosticIntelligenceRequest
    {
        public string? SymptomQuery { get; init; }
        public Guid? VehicleId { get; init; }
        public Guid? WorkOrderId { get; init; }
        public bool HasFinancePermission { get; init; }
    }

    public sealed class DiagnosticIntelligenceResult
    {
        public string Disclaimer { get; init; } = "Evidências relacionadas — NÃO é diagnóstico garantido.";
        public IReadOnlyList<EvidenceItem> RelatedEvidence { get; init; } = Array.Empty<EvidenceItem>();
        public IReadOnlyList<string> RelatedProceduresD01D17 { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> MissingData { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public bool IsGuaranteedDiagnosis => false;
        public string Provider { get; init; } = "PRIMOX_DIAGNOSTIC_INTELLIGENCE";
    }

    /// <summary>C2.7 — presents related evidence (cases/KB/D01-D17/context). Never claims guaranteed diagnosis.</summary>
    public sealed class DiagnosticIntelligenceService : IDiagnosticIntelligenceService
    {
        private readonly IContextualSearchService _search;
        private readonly IContextCompositionService _composition;
        private readonly IKnowledgeRetrievalService _retrieval;

        public DiagnosticIntelligenceService(
            IContextualSearchService search,
            IContextCompositionService composition,
            IKnowledgeRetrievalService retrieval)
        {
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _composition = composition ?? throw new ArgumentNullException(nameof(composition));
            _retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
        }

        public async Task<DiagnosticIntelligenceResult> AnalyzeAsync(DiagnosticIntelligenceRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            var warnings = new List<string> { "NOT_A_GUARANTEED_DIAGNOSIS" };
            var missing = new List<string>();

            var composed = await _composition.ComposeAsync(new ContextCompositionRequest
            {
                Query = request.SymptomQuery,
                VehicleId = request.VehicleId,
                WorkOrderId = request.WorkOrderId,
                IncludeFinancial = request.HasFinancePermission
            }, ct).ConfigureAwait(false);
            missing.AddRange(composed.MissingData);

            var query = string.IsNullOrWhiteSpace(request.SymptomQuery)
                ? (composed.WorkOrder?.Facts.FirstOrDefault(f => f.FactKey == "os.problema")?.Value
                   ?? composed.Vehicle?.Facts.FirstOrDefault(f => f.FactKey == "vehicle.problema_recorrente")?.Value
                   ?? string.Empty)
                : request.SymptomQuery!;

            if (string.IsNullOrWhiteSpace(query))
            {
                missing.Add("Sintoma / query diagnóstica");
                return new DiagnosticIntelligenceResult
                {
                    RelatedEvidence = Array.Empty<EvidenceItem>(),
                    MissingData = missing.Distinct().ToList(),
                    Warnings = warnings
                };
            }

            var search = await _search.SearchAsync(new KnowledgeSearchQuery
            {
                Text = query,
                UserContext = new KnowledgeSearchUserContext
                {
                    VehicleId = request.VehicleId,
                    WorkOrderId = request.WorkOrderId,
                    HasFinancePermission = request.HasFinancePermission
                },
                MaxResults = 15
            }, ct).ConfigureAwait(false);

            var evidence = search.Results.SelectMany(r => r.Evidence ?? Array.Empty<EvidenceItem>()).Take(20).ToList();
            await _retrieval.RebuildIndexAsync(ct).ConfigureAwait(false);
            var dCodes = _retrieval.GetIndexedProceduresD01ToD17()
                .Where(p => evidence.Any(e => e.SourceCode.Equals(p.Code, StringComparison.OrdinalIgnoreCase))
                            || (!string.IsNullOrWhiteSpace(query) && (
                                (p.Title?.Contains(query.Split(' ').FirstOrDefault() ?? "___", StringComparison.OrdinalIgnoreCase) ?? false)
                                || (p.Symptom?.IndexOf(query.Split(' ').FirstOrDefault() ?? "___", StringComparison.OrdinalIgnoreCase) >= 0))))
                .Select(p => p.Code)
                .Distinct()
                .Take(17)
                .ToList();

            if (evidence.Count == 0)
            {
                warnings.Add("NO_RELATED_EVIDENCE");
            }

            return new DiagnosticIntelligenceResult
            {
                RelatedEvidence = evidence,
                RelatedProceduresD01D17 = dCodes,
                MissingData = missing.Distinct().ToList(),
                Warnings = warnings
            };
        }
    }
}