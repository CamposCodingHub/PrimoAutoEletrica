using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// C4.3 Diagnostic Reasoning Foundation.
    /// Every PossibleCause is a HYPOTHESIS — never a confirmed diagnosis.
    /// Flow: Symptom + Evidence → ranked hypotheses → recommended checks → human decision.
    /// </summary>
    public enum DiagnosticHypothesisStatus
    {
        HYPOTHESIS = 0,
        // Confirmed diagnosis is intentionally absent from this foundation.
    }

    public sealed class DiagnosticHypothesis
    {
        public string HypothesisId { get; init; } = string.Empty;
        public string Symptom { get; init; } = string.Empty;
        public IReadOnlyList<string> EvidenceIds { get; init; } = Array.Empty<string>();
        public string PossibleCause { get; init; } = string.Empty;
        /// <summary>Ordinal confidence label only — NOT a probability of fault / confirmed diagnosis.</summary>
        public string Confidence { get; init; } = "LOW";
        public string RecommendedCheck { get; init; } = string.Empty;
        public IReadOnlyList<string> RelatedKnowledgeIds { get; init; } = Array.Empty<string>();
        public string Source { get; init; } = string.Empty;
        public DiagnosticHypothesisStatus Status { get; init; } = DiagnosticHypothesisStatus.HYPOTHESIS;
        public string Disclaimer { get; init; } = "HYPOTHESIS only — not a confirmed diagnosis. Human decision required.";
    }

    public sealed class DiagnosticReasoningRequest
    {
        public string? CaseId { get; init; }
        public string? Symptom { get; init; }
        public Guid? ClienteId { get; init; }
        public Guid? VehicleId { get; init; }
        public Guid? WorkOrderId { get; init; }
        public Guid? SessionClienteId { get; init; }
        public bool CanIncludeFinance { get; init; }
        public IReadOnlyList<RankedEvidenceItem> RankedEvidence { get; init; } = Array.Empty<RankedEvidenceItem>();
        public SourceTaggedContextPackage? ContextPackage { get; init; }
    }

    public sealed class DiagnosticReasoningResult
    {
        public string CaseId { get; init; } = string.Empty;
        public string Symptom { get; init; } = string.Empty;
        public IReadOnlyList<DiagnosticHypothesis> Hypotheses { get; init; } = Array.Empty<DiagnosticHypothesis>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> MissingData { get; init; } = Array.Empty<string>();
        public bool IsConfirmedDiagnosis => false;
        public string Provider { get; init; } = "PRIMOX_DIAGNOSTIC_REASONING_C4_3";
    }

    public interface IDiagnosticReasoningService
    {
        Task<DiagnosticReasoningResult> ReasonAsync(DiagnosticReasoningRequest request, CancellationToken ct = default);
        IReadOnlyList<DiagnosticFixtureCase> GetFixtureCases();
    }

    public sealed class DiagnosticFixtureCase
    {
        public string CaseId { get; init; } = string.Empty;
        public string Symptom { get; init; } = string.Empty;
        public string ExpectedCauseHint { get; init; } = string.Empty;
        public string RecommendedCheck { get; init; } = string.Empty;
        public string RelatedKnowledgeHint { get; init; } = string.Empty;
    }

    public sealed class DiagnosticReasoningService : IDiagnosticReasoningService
    {
        public const string WarningNotConfirmed = "NOT_A_CONFIRMED_DIAGNOSIS";
        public const string WarningCrossClient = "CROSS_CLIENT_DENIED";
        public const string WarningNoSymptom = "SYMPTOM_MISSING";
        public const string WarningNoEvidence = "NO_EVIDENCE_FOR_HYPOTHESIS";

        private static readonly DiagnosticFixtureCase[] FixtureCases =
        {
            new() { CaseId = "DIAG-001", Symptom = "alternador nao carrega", ExpectedCauseHint = "regulador ou escovas", RecommendedCheck = "medir tensao em carga 13.8-14.7V", RelatedKnowledgeHint = "alternador" },
            new() { CaseId = "DIAG-002", Symptom = "bateria descarrega overnight", ExpectedCauseHint = "consumo parasitico", RecommendedCheck = "medir corrente de fuga com amperimetro", RelatedKnowledgeHint = "bateria" },
            new() { CaseId = "DIAG-003", Symptom = "motor de partida nao gira", ExpectedCauseHint = "solenoide ou massa", RecommendedCheck = "teste de queda de tensao no circuito de partida", RelatedKnowledgeHint = "partida" },
            new() { CaseId = "DIAG-004", Symptom = "farol oscila com aceleracao", ExpectedCauseHint = "regulador de tensao", RecommendedCheck = "osciloscopio / tensao alternador vs RPM", RelatedKnowledgeHint = "regulador" },
            new() { CaseId = "DIAG-005", Symptom = "fusivel queima recorrente", ExpectedCauseHint = "curto intermitente", RecommendedCheck = "mapa de circuito e isolamento do ramo", RelatedKnowledgeHint = "fusivel" },
            new() { CaseId = "DIAG-006", Symptom = "sensor ABS falha intermitente", ExpectedCauseHint = "chicote ou entreferro", RecommendedCheck = "resistencia e sinal do sensor com roda girando", RelatedKnowledgeHint = "ABS" },
            new() { CaseId = "DIAG-007", Symptom = "codigo P0562 baixa tensao", ExpectedCauseHint = "carga insuficiente", RecommendedCheck = "carga do alternador e queda no cabo B+", RelatedKnowledgeHint = "P0562" },
            new() { CaseId = "DIAG-008", Symptom = "luz de carga acesa em marcha lenta", ExpectedCauseHint = "escovas gastas", RecommendedCheck = "inspecao de escovas e aneis coletores", RelatedKnowledgeHint = "luz de carga" },
        };

        private readonly IDiagnosticIntelligenceService? _legacy;
        private readonly IEvidenceRankingService? _ranking;
        private readonly IIntelligenceAuditService? _audit;

        public DiagnosticReasoningService(
            IDiagnosticIntelligenceService? legacy = null,
            IEvidenceRankingService? ranking = null,
            IIntelligenceAuditService? audit = null)
        {
            _legacy = legacy;
            _ranking = ranking;
            _audit = audit;
        }

        public IReadOnlyList<DiagnosticFixtureCase> GetFixtureCases() => FixtureCases;

        public async Task<DiagnosticReasoningResult> ReasonAsync(DiagnosticReasoningRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ct.ThrowIfCancellationRequested();

            var warnings = new List<string> { WarningNotConfirmed };
            var missing = new List<string>();

            if (request.SessionClienteId.HasValue &&
                request.ClienteId.HasValue &&
                request.SessionClienteId.Value != Guid.Empty &&
                request.ClienteId.Value != Guid.Empty &&
                request.SessionClienteId.Value != request.ClienteId.Value)
            {
                warnings.Add(WarningCrossClient);
                return new DiagnosticReasoningResult
                {
                    CaseId = request.CaseId ?? string.Empty,
                    Symptom = request.Symptom ?? string.Empty,
                    Hypotheses = Array.Empty<DiagnosticHypothesis>(),
                    Warnings = warnings,
                    MissingData = new[] { "cliente compatível com sessão" }
                };
            }

            var symptom = (request.Symptom ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(symptom) && !string.IsNullOrWhiteSpace(request.CaseId))
            {
                var fx = FixtureCases.FirstOrDefault(c =>
                    string.Equals(c.CaseId, request.CaseId, StringComparison.OrdinalIgnoreCase));
                if (fx != null) symptom = fx.Symptom;
            }

            if (string.IsNullOrWhiteSpace(symptom))
            {
                warnings.Add(WarningNoSymptom);
                missing.Add("Symptom");
                return new DiagnosticReasoningResult
                {
                    CaseId = request.CaseId ?? string.Empty,
                    Hypotheses = Array.Empty<DiagnosticHypothesis>(),
                    Warnings = warnings,
                    MissingData = missing
                };
            }

            var evidence = request.RankedEvidence?.ToList() ?? new List<RankedEvidenceItem>();
            if (evidence.Count == 0 && _ranking != null)
            {
                var ranked = await _ranking.RankAsync(new EvidenceRankingRequest
                {
                    Query = symptom,
                    SessionClienteId = request.SessionClienteId,
                    ScopedClienteId = request.ClienteId,
                    ScopedVeiculoId = request.VehicleId,
                    ScopedWorkOrderId = request.WorkOrderId,
                    CanIncludeFinance = request.CanIncludeFinance,
                    IncludeFinancial = request.CanIncludeFinance,
                    ContextPackage = request.ContextPackage,
                    Candidates = Array.Empty<EvidenceItem>()
                }, ct).ConfigureAwait(false);
                evidence.AddRange(ranked.Ranked);
                warnings.AddRange(ranked.Warnings);
            }

            if (_legacy != null)
            {
                var legacy = await _legacy.AnalyzeAsync(new DiagnosticIntelligenceRequest
                {
                    SymptomQuery = symptom,
                    VehicleId = request.VehicleId,
                    WorkOrderId = request.WorkOrderId,
                    HasFinancePermission = request.CanIncludeFinance
                }, ct).ConfigureAwait(false);
                warnings.AddRange(legacy.Warnings);
                missing.AddRange(legacy.MissingData);
                foreach (var ev in legacy.RelatedEvidence ?? Array.Empty<EvidenceItem>())
                {
                    if (evidence.Any(e => e.EvidenceId == ev.EvidenceId)) continue;
                    evidence.Add(new RankedEvidenceItem
                    {
                        EvidenceId = ev.EvidenceId,
                        SourceType = ev.Kind.ToString(),
                        SourceId = ev.SourceCode,
                        Title = ev.Title,
                        Excerpt = ev.Excerpt,
                        RelevanceScore = ev.RelevanceScore ?? 0,
                        RankingReason = ev.RelevanceLabel ?? "legacy-diagnostic",
                        Classification = ev.Classification
                    });
                }
            }

            var hypotheses = BuildHypotheses(request.CaseId, symptom, evidence);
            if (hypotheses.Count == 0)
            {
                warnings.Add(WarningNoEvidence);
            }

            var result = new DiagnosticReasoningResult
            {
                CaseId = request.CaseId ?? InferCaseId(symptom),
                Symptom = symptom,
                Hypotheses = hypotheses,
                Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                MissingData = missing.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            };

            _audit?.Record(new IntelligenceAuditEntry
            {
                Question = symptom,
                ContextSummary = $"C4.3 hypotheses={result.Hypotheses.Count} confirmed=false",
                EvidenceIds = string.Join(",", result.Hypotheses.SelectMany(h => h.EvidenceIds).Distinct()),
                Provider = "LOCAL/C4.3",
                Result = "DIAGNOSTIC_HYPOTHESES",
                Status = "OK"
            });

            return result;
        }

        private static string InferCaseId(string symptom)
        {
            var match = FixtureCases.FirstOrDefault(c =>
                symptom.Contains(c.Symptom.Split(' ').First(), StringComparison.OrdinalIgnoreCase));
            return match?.CaseId ?? "DIAG-ADHOC";
        }

        private static List<DiagnosticHypothesis> BuildHypotheses(
            string? caseId,
            string symptom,
            IReadOnlyList<RankedEvidenceItem> evidence)
        {
            var list = new List<DiagnosticHypothesis>();
            var fixtures = FixtureCases
                .Where(f =>
                    string.Equals(f.CaseId, caseId, StringComparison.OrdinalIgnoreCase) ||
                    TokenOverlap(symptom, f.Symptom) >= 1)
                .Take(3)
                .ToList();

            if (fixtures.Count == 0 && evidence.Count > 0)
            {
                // Evidence-grounded ad-hoc hypothesis only — still HYPOTHESIS
                var top = evidence.OrderByDescending(e => e.RelevanceScore).Take(3).ToList();
                list.Add(new DiagnosticHypothesis
                {
                    HypothesisId = "HYP-ADHOC-1",
                    Symptom = symptom,
                    EvidenceIds = top.Select(e => e.EvidenceId).ToList(),
                    PossibleCause = $"Possível causa relacionada a: {top[0].Title}",
                    Confidence = MapConfidence(top[0].RelevanceScore),
                    RecommendedCheck = "Validar com medição no veículo antes de qualquer ação",
                    RelatedKnowledgeIds = top.Select(e => e.SourceId).Where(s => !string.IsNullOrWhiteSpace(s)).ToList(),
                    Source = top[0].SourceType + ":" + top[0].SourceId,
                    Status = DiagnosticHypothesisStatus.HYPOTHESIS
                });
                return list;
            }

            foreach (var fx in fixtures)
            {
                var related = evidence
                    .Where(e =>
                        TokenOverlap(e.Title + " " + e.Excerpt, fx.RelatedKnowledgeHint) >= 1 ||
                        TokenOverlap(e.Title + " " + e.Excerpt, fx.Symptom) >= 1 ||
                        TokenOverlap(e.Title + " " + e.Excerpt, symptom) >= 1)
                    .OrderByDescending(e => e.RelevanceScore)
                    .Take(5)
                    .ToList();

                var conf = related.Count switch
                {
                    >= 3 => "MEDIUM",
                    >= 1 => "LOW",
                    _ => "LOW"
                };

                list.Add(new DiagnosticHypothesis
                {
                    HypothesisId = $"HYP-{fx.CaseId}",
                    Symptom = symptom,
                    EvidenceIds = related.Select(e => e.EvidenceId).ToList(),
                    PossibleCause = fx.ExpectedCauseHint,
                    Confidence = related.Count == 0 ? "LOW" : conf,
                    RecommendedCheck = fx.RecommendedCheck,
                    RelatedKnowledgeIds = related.Select(e => e.SourceId).Distinct().ToList(),
                    Source = related.Count > 0
                        ? string.Join(",", related.Select(e => $"{e.SourceType}:{e.SourceId}"))
                        : $"fixture:{fx.CaseId}",
                    Status = DiagnosticHypothesisStatus.HYPOTHESIS
                });
            }

            return list;
        }

        private static string MapConfidence(double score) =>
            score >= 8 ? "MEDIUM" : "LOW"; // never HIGH without human confirmation in C4.3 foundation

        private static int TokenOverlap(string a, string b)
        {
            var ta = EvidenceRankingService.Tokenize(a);
            var tb = EvidenceRankingService.Tokenize(b);
            return ta.Intersect(tb, StringComparer.Ordinal).Count();
        }
    }
}
