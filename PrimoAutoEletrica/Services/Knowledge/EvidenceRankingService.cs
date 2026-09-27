using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.ExternalAi;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// C4.2 Evidence Ranking — AUTHORIZATION → FILTER → RETRIEVAL → RANKING.
    /// Scores are explainable component sums (no magic). Suggest-only; never auto-action.
    /// Cross-client evidence is never retrieved.
    /// </summary>
    public sealed class RankedEvidenceItem
    {
        public string EvidenceId { get; init; } = string.Empty;
        public string SourceType { get; init; } = string.Empty;
        public string SourceId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Excerpt { get; init; } = string.Empty;
        public double RelevanceScore { get; init; }
        public string RankingReason { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, double> ScoreComponents { get; init; } =
            new Dictionary<string, double>();
        public string Classification { get; init; } = "TECHNICAL";
        public Guid? ClienteAnchorId { get; init; }
        public Guid? VeiculoAnchorId { get; init; }
        public Guid? WorkOrderAnchorId { get; init; }
    }

    public sealed class EvidenceRankingRequest
    {
        public string Query { get; init; } = string.Empty;
        public Guid? SessionClienteId { get; init; }
        public Guid? ScopedClienteId { get; init; }
        public Guid? ScopedVeiculoId { get; init; }
        public Guid? ScopedWorkOrderId { get; init; }
        public bool IncludeFinancial { get; init; }
        public bool CanIncludeFinance { get; init; }
        public int MaxResults { get; init; } = 20;
        public IReadOnlyList<EvidenceItem> Candidates { get; init; } = Array.Empty<EvidenceItem>();
        public SourceTaggedContextPackage? ContextPackage { get; init; }
        public ExternalEvidencePackage? EvidencePackage { get; init; }
    }

    public sealed class EvidenceRankingResult
    {
        public string Pipeline { get; init; } = "AUTHORIZATION→FILTER→RETRIEVAL→RANKING";
        public string Query { get; init; } = string.Empty;
        public IReadOnlyList<RankedEvidenceItem> Ranked { get; init; } = Array.Empty<RankedEvidenceItem>();
        public IReadOnlyList<string> FilteredOutReasons { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public bool AuthorizationOk { get; init; } = true;
        public bool Deterministic { get; init; } = true;
        public string ScoreModelVersion { get; init; } = EvidenceRankingService.ScoreModelVersion;
    }

    public interface IEvidenceRankingService
    {
        Task<EvidenceRankingResult> RankAsync(EvidenceRankingRequest request, CancellationToken ct = default);
    }

    public sealed class EvidenceRankingService : IEvidenceRankingService
    {
        public const string ScoreModelVersion = "C4.2-EXPLAINABLE-V1";
        public const string WarningCrossClientBlocked = "CROSS_CLIENT_EVIDENCE_BLOCKED";
        public const string WarningAuthDenied = "AUTHORIZATION_DENIED";
        public const string WarningFinanceFiltered = "FINANCE_EVIDENCE_FILTERED";
        public const string WarningNoCandidates = "NO_EVIDENCE_CANDIDATES";

        // Documented component weights (no magic opaque ML score)
        public const double WeightTokenOverlap = 1.0;
        public const double WeightTitleHit = 2.0;
        public const double WeightSourceTypeKnowledge = 1.5;
        public const double WeightSourceTypeDiagnostic = 1.4;
        public const double WeightSourceTypeProcedure = 1.3;
        public const double WeightSourceTypeWorkOrder = 1.2;
        public const double WeightSourceTypeOther = 1.0;
        public const double WeightAnchorCliente = 3.0;
        public const double WeightAnchorVeiculo = 2.5;
        public const double WeightAnchorOs = 2.0;

        private readonly IIntelligenceAuditService? _audit;

        public EvidenceRankingService(IIntelligenceAuditService? audit = null)
        {
            _audit = audit;
        }

        public Task<EvidenceRankingResult> RankAsync(EvidenceRankingRequest request, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ct.ThrowIfCancellationRequested();

            var warnings = new List<string>();
            var filtered = new List<string>();

            // 1) AUTHORIZATION
            if (request.SessionClienteId.HasValue &&
                request.ScopedClienteId.HasValue &&
                request.SessionClienteId.Value != Guid.Empty &&
                request.ScopedClienteId.Value != Guid.Empty &&
                request.SessionClienteId.Value != request.ScopedClienteId.Value)
            {
                warnings.Add(WarningAuthDenied);
                warnings.Add(WarningCrossClientBlocked);
                var denied = new EvidenceRankingResult
                {
                    Query = request.Query ?? string.Empty,
                    Ranked = Array.Empty<RankedEvidenceItem>(),
                    FilteredOutReasons = new[] { "session_cliente != scoped_cliente" },
                    Warnings = warnings,
                    AuthorizationOk = false
                };
                _audit?.Record(new IntelligenceAuditEntry
                {
                    Question = denied.Query,
                    ContextSummary = "C4.2 ranking AUTHORIZATION denied",
                    Provider = "LOCAL/C4.2",
                    Result = "RANKING_DENIED",
                    Status = "FAIL_CLOSED"
                });
                return Task.FromResult(denied);
            }

            var financeOk = request.IncludeFinancial && request.CanIncludeFinance;

            // 2) FILTER + 3) RETRIEVAL (from provided candidates / packages only — never invent)
            var raw = CollectCandidates(request, financeOk, filtered, warnings);
            var scopedClient = request.ScopedClienteId ?? request.SessionClienteId;

            var authorized = new List<Candidate>();
            foreach (var c in raw)
            {
                if (IsCrossClient(c, scopedClient))
                {
                    filtered.Add($"cross-client:{c.EvidenceId}:cliente={c.ClienteAnchorId}");
                    warnings.Add(WarningCrossClientBlocked);
                    continue;
                }
                authorized.Add(c);
            }

            if (authorized.Count == 0)
            {
                warnings.Add(WarningNoCandidates);
            }

            // 4) RANKING — deterministic explainable components
            var tokens = Tokenize(request.Query);
            var ranked = authorized
                .Select(c => Score(c, tokens, request))
                .OrderByDescending(r => r.RelevanceScore)
                .ThenBy(r => r.EvidenceId, StringComparer.Ordinal)
                .ThenBy(r => r.SourceId, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(1, request.MaxResults))
                .ToList();

            // Stability check: re-score same input → same order (deterministic by construction)
            var result = new EvidenceRankingResult
            {
                Query = request.Query ?? string.Empty,
                Ranked = ranked,
                FilteredOutReasons = filtered.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                AuthorizationOk = true,
                Deterministic = true
            };

            _audit?.Record(new IntelligenceAuditEntry
            {
                Question = result.Query,
                ContextSummary = $"C4.2 ranked={result.Ranked.Count} filtered={result.FilteredOutReasons.Count}",
                EvidenceIds = string.Join(",", result.Ranked.Select(r => r.EvidenceId)),
                EvidenceSummary = ScoreModelVersion,
                Provider = "LOCAL/C4.2",
                Result = "EVIDENCE_RANKED",
                Status = "OK"
            });

            return Task.FromResult(result);
        }

        private sealed class Candidate
        {
            public string EvidenceId { get; init; } = string.Empty;
            public string SourceType { get; init; } = string.Empty;
            public string SourceId { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string Excerpt { get; init; } = string.Empty;
            public string Classification { get; init; } = "TECHNICAL";
            public Guid? ClienteAnchorId { get; init; }
            public Guid? VeiculoAnchorId { get; init; }
            public Guid? WorkOrderAnchorId { get; init; }
            public double? PriorScore { get; init; }
        }

        private static List<Candidate> CollectCandidates(
            EvidenceRankingRequest request,
            bool financeOk,
            List<string> filtered,
            List<string> warnings)
        {
            var list = new List<Candidate>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(Candidate c)
            {
                if (string.IsNullOrWhiteSpace(c.EvidenceId)) return;
                if (!seen.Add(c.EvidenceId)) return;
                if (!financeOk &&
                    string.Equals(c.Classification, "FINANCIAL", StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add($"finance:{c.EvidenceId}");
                    warnings.Add(WarningFinanceFiltered);
                    return;
                }
                list.Add(c);
            }

            foreach (var ev in request.Candidates ?? Array.Empty<EvidenceItem>())
            {
                if (ev == null) continue;
                Add(new Candidate
                {
                    EvidenceId = ev.EvidenceId,
                    SourceType = ev.Kind.ToString(),
                    SourceId = string.IsNullOrWhiteSpace(ev.SourceCode) ? ev.EvidenceId : ev.SourceCode,
                    Title = ev.Title ?? string.Empty,
                    Excerpt = ev.Excerpt ?? string.Empty,
                    Classification = ev.Classification ?? "TECHNICAL",
                    PriorScore = ev.RelevanceScore,
                    ClienteAnchorId = request.ScopedClienteId,
                    VeiculoAnchorId = request.ScopedVeiculoId,
                    WorkOrderAnchorId = request.ScopedWorkOrderId
                });
            }

            if (request.EvidencePackage?.Sources != null)
            {
                foreach (var src in request.EvidencePackage.Sources)
                {
                    Add(new Candidate
                    {
                        EvidenceId = src.EvidenceId,
                        SourceType = src.SourceType ?? string.Empty,
                        SourceId = src.SourceId ?? string.Empty,
                        Title = src.Title ?? string.Empty,
                        Excerpt = src.Excerpt ?? string.Empty,
                        Classification = src.Classification ?? "TECHNICAL",
                        ClienteAnchorId = request.ScopedClienteId,
                        VeiculoAnchorId = request.ScopedVeiculoId,
                        WorkOrderAnchorId = request.ScopedWorkOrderId
                    });
                }
            }

            if (request.ContextPackage?.Items != null)
            {
                foreach (var item in request.ContextPackage.Items)
                {
                    if (item.SourceTag is not (ContextSourceTag.KNOWLEDGE or ContextSourceTag.EVIDENCE
                        or ContextSourceTag.ASSIST_LOCAL or ContextSourceTag.EXTERNAL_EVIDENCE
                        or ContextSourceTag.DIAGNOSTIC))
                        continue;

                    var id = !string.IsNullOrWhiteSpace(item.EvidenceId) ? item.EvidenceId : item.SourceId;
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    Guid? clientAnchor = null;
                    if (Guid.TryParse(item.IsolationAnchorId, out var g) && g != Guid.Empty)
                        clientAnchor = g;

                    Add(new Candidate
                    {
                        EvidenceId = id,
                        SourceType = item.SourceType ?? item.SourceTag.ToString(),
                        SourceId = item.SourceId ?? id,
                        Title = item.Key ?? string.Empty,
                        Excerpt = item.Value ?? string.Empty,
                        Classification = item.Classification ?? "TECHNICAL",
                        ClienteAnchorId = clientAnchor ?? request.ContextPackage.ScopedClienteId,
                        VeiculoAnchorId = request.ContextPackage.ScopedVeiculoId,
                        WorkOrderAnchorId = request.ContextPackage.ScopedWorkOrderId
                    });
                }
            }

            return list;
        }

        private static bool IsCrossClient(Candidate c, Guid? scopedClient)
        {
            if (!scopedClient.HasValue || scopedClient.Value == Guid.Empty) return false;
            if (!c.ClienteAnchorId.HasValue || c.ClienteAnchorId.Value == Guid.Empty) return false;
            return c.ClienteAnchorId.Value != scopedClient.Value;
        }

        public static RankedEvidenceItem Score(CandidateLike c, IReadOnlyList<string> tokens, EvidenceRankingRequest request)
            => Score(new Candidate
            {
                EvidenceId = c.EvidenceId,
                SourceType = c.SourceType,
                SourceId = c.SourceId,
                Title = c.Title,
                Excerpt = c.Excerpt,
                Classification = c.Classification,
                ClienteAnchorId = c.ClienteAnchorId,
                VeiculoAnchorId = c.VeiculoAnchorId,
                WorkOrderAnchorId = c.WorkOrderAnchorId,
                PriorScore = c.PriorScore
            }, tokens, request);

        /// <summary>Public test helper shape.</summary>
        public sealed class CandidateLike
        {
            public string EvidenceId { get; init; } = string.Empty;
            public string SourceType { get; init; } = string.Empty;
            public string SourceId { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string Excerpt { get; init; } = string.Empty;
            public string Classification { get; init; } = "TECHNICAL";
            public Guid? ClienteAnchorId { get; init; }
            public Guid? VeiculoAnchorId { get; init; }
            public Guid? WorkOrderAnchorId { get; init; }
            public double? PriorScore { get; init; }
        }

        private static RankedEvidenceItem Score(Candidate c, IReadOnlyList<string> tokens, EvidenceRankingRequest request)
        {
            var components = new Dictionary<string, double>(StringComparer.Ordinal);
            var hayTitle = (c.Title ?? string.Empty).ToLowerInvariant();
            var hayExcerpt = (c.Excerpt ?? string.Empty).ToLowerInvariant();
            var hayAll = hayTitle + " " + hayExcerpt;

            double tokenOverlap = 0;
            double titleHits = 0;
            foreach (var t in tokens)
            {
                if (string.IsNullOrEmpty(t)) continue;
                if (hayAll.Contains(t, StringComparison.Ordinal))
                    tokenOverlap += WeightTokenOverlap;
                if (hayTitle.Contains(t, StringComparison.Ordinal))
                    titleHits += WeightTitleHit;
            }
            components["token_overlap"] = tokenOverlap;
            components["title_hit"] = titleHits;

            var sourceBoost = ResolveSourceTypeWeight(c.SourceType);
            components["source_type_boost"] = sourceBoost;

            double anchor = 0;
            if (request.ScopedClienteId.HasValue && c.ClienteAnchorId == request.ScopedClienteId)
            {
                anchor += WeightAnchorCliente;
                components["anchor_cliente"] = WeightAnchorCliente;
            }
            if (request.ScopedVeiculoId.HasValue && c.VeiculoAnchorId == request.ScopedVeiculoId)
            {
                anchor += WeightAnchorVeiculo;
                components["anchor_veiculo"] = WeightAnchorVeiculo;
            }
            if (request.ScopedWorkOrderId.HasValue && c.WorkOrderAnchorId == request.ScopedWorkOrderId)
            {
                anchor += WeightAnchorOs;
                components["anchor_os"] = WeightAnchorOs;
            }

            // Prior retrieval score is additive only when already present (never invent)
            if (c.PriorScore.HasValue)
            {
                components["prior_retrieval"] = c.PriorScore.Value;
            }

            var total = components.Values.Sum();
            var reason = string.Join(" + ", components.Select(kv =>
                $"{kv.Key}={kv.Value.ToString("0.##", CultureInfo.InvariantCulture)}"))
                + $" => {total.ToString("0.##", CultureInfo.InvariantCulture)} ({ScoreModelVersion})";

            return new RankedEvidenceItem
            {
                EvidenceId = c.EvidenceId,
                SourceType = c.SourceType,
                SourceId = c.SourceId,
                Title = c.Title,
                Excerpt = c.Excerpt,
                RelevanceScore = total,
                RankingReason = reason,
                ScoreComponents = components,
                Classification = c.Classification,
                ClienteAnchorId = c.ClienteAnchorId,
                VeiculoAnchorId = c.VeiculoAnchorId,
                WorkOrderAnchorId = c.WorkOrderAnchorId
            };
        }

        public static double ResolveSourceTypeWeight(string? sourceType)
        {
            if (string.IsNullOrWhiteSpace(sourceType)) return WeightSourceTypeOther;
            var t = sourceType.Trim();
            if (t.Contains("Knowledge", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Article", StringComparison.OrdinalIgnoreCase))
                return WeightSourceTypeKnowledge;
            if (t.Contains("Diagnostic", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Case", StringComparison.OrdinalIgnoreCase))
                return WeightSourceTypeDiagnostic;
            if (t.Contains("Procedure", StringComparison.OrdinalIgnoreCase) ||
                t.StartsWith("D0", StringComparison.OrdinalIgnoreCase))
                return WeightSourceTypeProcedure;
            if (t.Contains("WorkOrder", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("OS", StringComparison.OrdinalIgnoreCase))
                return WeightSourceTypeWorkOrder;
            return WeightSourceTypeOther;
        }

        public static IReadOnlyList<string> Tokenize(string? query)
        {
            if (string.IsNullOrWhiteSpace(query)) return Array.Empty<string>();
            var normalized = query.Trim().ToLowerInvariant();
            var sb = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            }
            return sb.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length >= 2)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToList();
        }
    }
}
