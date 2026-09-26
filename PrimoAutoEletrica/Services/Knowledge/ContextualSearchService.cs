using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IContextualSearchService
    {
        Task<KnowledgeSearchResponse> SearchAsync(KnowledgeSearchQuery query, CancellationToken ct = default);
    }

    /// <summary>
    /// C2.4 — Contextual Search: C2.2 deterministic search + C2.3 proven context.
    /// Grounded queries (vehicle history, diagnostics, parts, similar history, D01-D17, last service)
    /// only return evidence when Context Engine proves relations. No evidence → clear empty.
    /// </summary>
    public sealed class ContextualSearchService : IContextualSearchService
    {
        public const string WarningNoContextEvidence = "NO_CONTEXT_EVIDENCE";
        public const string WarningContextPartial = "CONTEXT_PARTIAL";
        public const string IntentVehicleHistory = "VEHICLE_HISTORY";
        public const string IntentDiagnostics = "DIAGNOSTICS";
        public const string IntentParts = "PARTS_USED";
        public const string IntentSimilar = "SIMILAR_HISTORY";
        public const string IntentLastService = "LAST_SERVICE";
        public const string IntentProcedure = "PROCEDURE_Dxx";
        public const string IntentGeneric = "GENERIC";

        private readonly IKnowledgeSearchService _search;
        private readonly IContextCompositionService _composition;
        private readonly LoggerService? _logger;

        public ContextualSearchService(
            IKnowledgeSearchService search,
            IContextCompositionService composition,
            LoggerService? logger = null)
        {
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _composition = composition ?? throw new ArgumentNullException(nameof(composition));
            _logger = logger;
        }

        public async Task<KnowledgeSearchResponse> SearchAsync(KnowledgeSearchQuery query, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            var intent = DetectIntent(query.Text ?? string.Empty);
            var user = query.UserContext;
            var hasAnchorIds =
                (user?.VehicleId.HasValue == true && user.VehicleId.Value != Guid.Empty) ||
                (user?.WorkOrderId.HasValue == true && user.WorkOrderId.Value != Guid.Empty) ||
                (user?.ClienteId.HasValue == true && user.ClienteId.Value != Guid.Empty);

            ComposedIntelligenceContext? composed = null;
            if (hasAnchorIds)
            {
                composed = await _composition.ComposeAsync(new ContextCompositionRequest
                {
                    Query = query.Text,
                    VehicleId = user!.VehicleId,
                    WorkOrderId = user.WorkOrderId,
                    ClienteId = user.ClienteId,
                    IncludeFinancial = user.HasFinancePermission || (query.HasFinancePermission == true),
                    SessionClienteId = null
                }, ct).ConfigureAwait(false);
            }

            // Contextual intents that require a proven vehicle/OS/client anchor
            if (RequiresProvenAnchor(intent) && !hasAnchorIds)
            {
                return EmptyContextResponse(
                    query,
                    intent,
                    "Informe veículo, OS ou cliente com relação comprovável para esta consulta contextual.",
                    new[] { "VehicleId / WorkOrderId / ClienteId" },
                    new[] { WarningNoContextEvidence, $"INTENT:{intent}" });
            }

            if (RequiresProvenAnchor(intent) && composed is { HasAnyProvenAnchor: false })
            {
                return EmptyContextResponse(
                    query,
                    intent,
                    "Sem evidência contextual comprovada para a âncora informada.",
                    composed?.MissingData ?? Array.Empty<string>(),
                    Merge(composed?.Warnings, WarningNoContextEvidence, $"INTENT:{intent}"));
            }

            // Enrich query text with proven technical tokens (never invent; only from PROVEN facts).
            var enrichedText = EnrichQueryWithProvenContext(query.Text ?? string.Empty, intent, composed);
            var searchQuery = new KnowledgeSearchQuery
            {
                Text = enrichedText,
                TypeFilter = PreferTypeFilter(intent, query.TypeFilter),
                SystemFilter = query.SystemFilter,
                VehicleModelFilter = query.VehicleModelFilter ?? ProvenVehicleModel(composed),
                MaxResults = query.MaxResults,
                Limit = query.Limit,
                Offset = query.Offset,
                Filters = query.Filters,
                UserContext = query.UserContext,
                HasFinancePermission = query.HasFinancePermission
            };

            var response = await _search.SearchAsync(searchQuery, ct).ConfigureAwait(false);

            // Post-filter: for history/parts intents, keep only hits linked to proven anchors when available.
            var filtered = FilterByProvenAnchors(response.Results, intent, composed);
            var notes = new List<string>();
            if (composed != null)
            {
                notes.AddRange(composed.Conflicts);
                notes.AddRange(composed.UnprovenRelations.Select(r =>
                    $"{r.RelationType}:{r.Status}:{r.Reason}"));
                if (composed.UnprovenRelations.Count > 0)
                {
                    notes.Add(WarningContextPartial);
                }
            }
            notes.Add($"INTENT:{intent}");

            if (RequiresProvenAnchor(intent) && filtered.Count == 0)
            {
                return new KnowledgeSearchResponse
                {
                    Query = query.Text ?? string.Empty,
                    NormalizedQuery = response.NormalizedQuery,
                    Tokens = response.Tokens,
                    State = KnowledgeSearchUiState.NoResults,
                    Message = "Nenhuma evidência comprovada encontrada para o contexto informado.",
                    Results = Array.Empty<KnowledgeSearchMatch>(),
                    Groups = Array.Empty<KnowledgeSearchGroup>(),
                    TotalCount = 0,
                    IndexedItemCount = response.IndexedItemCount,
                    DurationMs = response.DurationMs,
                    Provider = "PRIMOX_CONTEXTUAL_SEARCH",
                    Warnings = Merge(response.Warnings, WarningNoContextEvidence),
                    ContextMissingData = composed?.MissingData ?? Array.Empty<string>(),
                    ContextNotes = notes,
                    UsedContext = composed != null
                };
            }

            var groups = filtered.Count == 0
                ? response.Groups
                : RebuildGroups(filtered);

            return new KnowledgeSearchResponse
            {
                Query = query.Text ?? string.Empty,
                NormalizedQuery = response.NormalizedQuery,
                Tokens = response.Tokens,
                State = filtered.Count == 0 ? response.State : KnowledgeSearchUiState.Found,
                Message = filtered.Count == 0
                    ? response.Message
                    : $"{filtered.Count} resultado(s) contextual(is).",
                Results = filtered.Count == 0 && !RequiresProvenAnchor(intent) ? response.Results : filtered,
                Groups = filtered.Count == 0 && !RequiresProvenAnchor(intent) ? response.Groups : groups,
                TotalCount = filtered.Count == 0 && !RequiresProvenAnchor(intent) ? response.TotalCount : filtered.Count,
                IndexedItemCount = response.IndexedItemCount,
                DurationMs = response.DurationMs,
                Provider = composed != null ? "PRIMOX_CONTEXTUAL_SEARCH" : response.Provider,
                Warnings = response.Warnings,
                ContextMissingData = composed?.MissingData ?? Array.Empty<string>(),
                ContextNotes = notes,
                UsedContext = composed != null
            };
        }

        public static string DetectIntent(string text)
        {
            var q = DeterministicKnowledgeIndex.RemoveDiacritics((text ?? string.Empty).ToLowerInvariant());
            if (string.IsNullOrWhiteSpace(q)) return IntentGeneric;
            if (System.Text.RegularExpressions.Regex.IsMatch(q, @"\bd([0-1][0-9]|1[0-7])\b"))
                return IntentProcedure;
            if (q.Contains("ultimo servico") || q.Contains("ultima os") || q.Contains("last service"))
                return IntentLastService;
            if (q.Contains("peca") || q.Contains("pecas usadas") || q.Contains("parts used"))
                return IntentParts;
            if (q.Contains("similar") || q.Contains("historico similar") || q.Contains("casos parecidos"))
                return IntentSimilar;
            if (q.Contains("diagnost") || q.Contains("causa") || q.Contains("sintoma"))
                return IntentDiagnostics;
            if (q.Contains("historico") || q.Contains("problemas do veiculo") || q.Contains("historico do veiculo") ||
                q.Contains("vehicle history"))
                return IntentVehicleHistory;
            return IntentGeneric;
        }

        private static bool RequiresProvenAnchor(string intent) =>
            intent is IntentVehicleHistory or IntentLastService or IntentParts or IntentSimilar;

        private static KnowledgeType? PreferTypeFilter(string intent, KnowledgeType? existing)
        {
            if (existing.HasValue) return existing;
            return intent switch
            {
                IntentProcedure => KnowledgeType.PROCEDURE,
                IntentDiagnostics => KnowledgeType.DIAGNOSTIC_CASE,
                IntentVehicleHistory => null,
                IntentSimilar => KnowledgeType.DIAGNOSTIC_CASE,
                _ => null
            };
        }

        private static string? ProvenVehicleModel(ComposedIntelligenceContext? composed)
        {
            var model = composed?.Vehicle?.Facts.FirstOrDefault(f =>
                f.FactKey == "vehicle.modelo" && f.Status == ContextProvenanceStatus.PROVEN)?.Value;
            return string.IsNullOrWhiteSpace(model) ? null : model;
        }

        private static string EnrichQueryWithProvenContext(string text, string intent, ComposedIntelligenceContext? composed)
        {
            if (composed == null || !composed.HasAnyProvenAnchor) return text;
            var extras = new List<string>();

            if (intent is IntentVehicleHistory or IntentDiagnostics or IntentSimilar or IntentLastService)
            {
                var symptom = composed.WorkOrder?.Facts.FirstOrDefault(f => f.FactKey == "os.problema" && f.Status == ContextProvenanceStatus.PROVEN)?.Value
                    ?? composed.Vehicle?.Facts.FirstOrDefault(f => f.FactKey == "vehicle.last_os_symptom" && f.Status == ContextProvenanceStatus.PROVEN)?.Value
                    ?? composed.Vehicle?.Facts.FirstOrDefault(f => f.FactKey == "vehicle.problema_recorrente" && f.Status == ContextProvenanceStatus.PROVEN)?.Value;
                if (!string.IsNullOrWhiteSpace(symptom)) extras.Add(symptom!);
            }

            if (intent == IntentParts)
            {
                extras.AddRange(composed.WorkOrder?.PartItemLabels ?? Array.Empty<string>());
            }

            if (intent == IntentLastService)
            {
                var last = composed.Vehicle?.Facts.FirstOrDefault(f => f.FactKey == "vehicle.last_os_number" && f.Status == ContextProvenanceStatus.PROVEN)?.Value
                    ?? composed.WorkOrder?.Facts.FirstOrDefault(f => f.FactKey == "os.numero" && f.Status == ContextProvenanceStatus.PROVEN)?.Value;
                if (!string.IsNullOrWhiteSpace(last)) extras.Add(last!);
            }

            if (extras.Count == 0) return text;
            // Keep original query; append proven tokens to widen deterministic retrieval without inventing.
            return $"{text} {string.Join(" ", extras.Distinct())}".Trim();
        }

        private static IReadOnlyList<KnowledgeSearchMatch> FilterByProvenAnchors(
            IReadOnlyList<KnowledgeSearchMatch> results,
            string intent,
            ComposedIntelligenceContext? composed)
        {
            if (composed == null || !RequiresProvenAnchor(intent)) return results;
            var provenOs = new HashSet<string>(
                (composed.Vehicle?.ProvenWorkOrderIds ?? Array.Empty<Guid>())
                    .Concat(composed.Client?.ProvenWorkOrderIds ?? Array.Empty<Guid>())
                    .Concat(composed.WorkOrder?.Found == true ? new[] { composed.WorkOrder.RequestedWorkOrderId } : Array.Empty<Guid>())
                    .Select(g => g.ToString("N")),
                StringComparer.OrdinalIgnoreCase);
            var provenCases = new HashSet<string>(
                (composed.Vehicle?.ProvenDiagnosticCaseIds ?? Array.Empty<Guid>())
                    .Concat(composed.WorkOrder?.ProvenDiagnosticCaseIds ?? Array.Empty<Guid>())
                    .Select(g => g.ToString("N")),
                StringComparer.OrdinalIgnoreCase);
            var model = ProvenVehicleModel(composed);

            var kept = new List<KnowledgeSearchMatch>();
            foreach (var m in results)
            {
                var item = m.Item;
                if (item == null)
                {
                    kept.Add(m);
                    continue;
                }

                // Procedures / KB may stay if token-matched (technical knowledge is not vehicle-owned).
                if (item.Type is KnowledgeType.PROCEDURE or KnowledgeType.TECHNICAL_CASE)
                {
                    kept.Add(m);
                    continue;
                }

                if (item.Type == KnowledgeType.DIAGNOSTIC_CASE)
                {
                    if (!string.IsNullOrWhiteSpace(item.SourceEntityId) &&
                        provenCases.Contains(item.SourceEntityId.Replace("-", "")))
                    {
                        kept.Add(m);
                        continue;
                    }
                    // Model-only match is NOT enough to claim "this vehicle's history"
                    if (intent is IntentVehicleHistory or IntentLastService)
                    {
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(model) &&
                        !string.IsNullOrWhiteSpace(item.VehicleModel) &&
                        item.VehicleModel.Contains(model, StringComparison.OrdinalIgnoreCase) &&
                        intent == IntentSimilar)
                    {
                        kept.Add(m);
                    }
                    continue;
                }

                if (item.Type == KnowledgeType.WORK_ORDER)
                {
                    if (!string.IsNullOrWhiteSpace(item.SourceEntityId) &&
                        provenOs.Contains(item.SourceEntityId.Replace("-", "")))
                    {
                        kept.Add(m);
                    }
                    continue;
                }

                kept.Add(m);
            }

            return kept;
        }

        private static IReadOnlyList<KnowledgeSearchGroup> RebuildGroups(IReadOnlyList<KnowledgeSearchMatch> ordered)
        {
            return ordered
                .GroupBy(m => m.GroupKey)
                .Where(g => g.Any())
                .Select(g => new KnowledgeSearchGroup
                {
                    Key = g.Key,
                    DisplayName = KnowledgeSearchGroups.DisplayName(g.Key),
                    Items = g.ToList()
                })
                .ToList();
        }

        private static KnowledgeSearchResponse EmptyContextResponse(
            KnowledgeSearchQuery query,
            string intent,
            string message,
            IReadOnlyList<string> missing,
            IReadOnlyList<string> warnings)
        {
            return new KnowledgeSearchResponse
            {
                Query = query.Text ?? string.Empty,
                NormalizedQuery = KnowledgeQueryNormalizer.Normalize(query.Text ?? string.Empty),
                Tokens = KnowledgeQueryNormalizer.Tokenize(query.Text ?? string.Empty),
                State = KnowledgeSearchUiState.NoResults,
                Message = message,
                Results = Array.Empty<KnowledgeSearchMatch>(),
                Groups = Array.Empty<KnowledgeSearchGroup>(),
                TotalCount = 0,
                Provider = "PRIMOX_CONTEXTUAL_SEARCH",
                Warnings = warnings,
                ContextMissingData = missing,
                ContextNotes = new[] { $"INTENT:{intent}" },
                UsedContext = true
            };
        }

        private static IReadOnlyList<string> Merge(IReadOnlyList<string>? a, params string[] b)
        {
            var list = new List<string>();
            if (a != null) list.AddRange(a);
            list.AddRange(b);
            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}