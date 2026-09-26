using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Services.Knowledge
{
    public interface IKnowledgeSearchService
    {
        Task<KnowledgeSearchResponse> SearchAsync(KnowledgeSearchQuery query, CancellationToken ct = default);
        Task<KnowledgeSearchResponse> SearchAsync(string text, CancellationToken ct = default);
        KnowledgeSearchMatch? GetBySourceId(string sourceType, string sourceId, bool hasFinancePermission);
        Task<int> EnsureIndexAsync(CancellationToken ct = default);
    }

    /// <summary>
    /// C2.2 Intelligent Search orchestration (deterministic retrieval — NOT "IA").
    /// Architecture: UI → VM → KnowledgeSearchService → KnowledgeRetrieval → Adapters/Repo.
    /// No SQL in XAML/code-behind. Never invents results.
    /// </summary>
    public sealed class KnowledgeSearchService : IKnowledgeSearchService
    {
        public const string EmptyQueryMessage = "Digite algo para pesquisar.";
        public const string NoResultsMessage = "Nenhum resultado encontrado no acervo PRIMOX.";
        public const string ErrorMessage = "Não foi possível concluir a pesquisa. Tente novamente.";
        public const string RbacFilteredWarning = "RBAC_FINANCIAL_FILTERED";

        private readonly IKnowledgeRetrievalService _retrieval;
        private readonly LoggerService? _logger;

        public KnowledgeSearchService(IKnowledgeRetrievalService retrieval, LoggerService? logger = null)
        {
            _retrieval = retrieval ?? throw new ArgumentNullException(nameof(retrieval));
            _logger = logger;
        }

        public Task<int> EnsureIndexAsync(CancellationToken ct = default) => _retrieval.RebuildIndexAsync(ct);

        public Task<KnowledgeSearchResponse> SearchAsync(string text, CancellationToken ct = default)
        {
            return SearchAsync(new KnowledgeSearchQuery { Text = text ?? string.Empty }, ct);
        }

        public async Task<KnowledgeSearchResponse> SearchAsync(KnowledgeSearchQuery query, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(query);
            var sw = Stopwatch.StartNew();
            var raw = query.Text ?? string.Empty;
            var normalized = KnowledgeQueryNormalizer.Normalize(raw);
            var tokens = KnowledgeQueryNormalizer.Tokenize(raw);

            if (KnowledgeQueryNormalizer.IsEmptyQuery(raw))
            {
                sw.Stop();
                return new KnowledgeSearchResponse
                {
                    Query = raw,
                    NormalizedQuery = normalized,
                    Tokens = tokens,
                    State = KnowledgeSearchUiState.NoQuery,
                    Message = EmptyQueryMessage,
                    DurationMs = sw.ElapsedMilliseconds,
                    Warnings = new[] { "EMPTY_QUERY" }
                };
            }

            try
            {
                ct.ThrowIfCancellationRequested();

                var limit = query.Limit > 0 ? query.Limit : (query.MaxResults > 0 ? query.MaxResults : 20);
                limit = Math.Min(Math.Max(limit, 1), 100);
                var offset = Math.Max(0, query.Offset);

                // Pull a wider window so Offset + phrase re-rank still work.
                var fetch = Math.Min(100, limit + offset + 20);
                var retrievalQuery = new KnowledgeSearchQuery
                {
                    Text = raw.Trim(),
                    TypeFilter = query.TypeFilter,
                    SystemFilter = query.SystemFilter ?? TryFilter(query.Filters, "system"),
                    VehicleModelFilter = query.VehicleModelFilter ?? TryFilter(query.Filters, "vehicle"),
                    MaxResults = fetch,
                    Limit = fetch,
                    UserContext = query.UserContext,
                    HasFinancePermission = query.HasFinancePermission
                };

                var retrieval = await _retrieval.SearchAsync(retrievalQuery, ct).ConfigureAwait(false);

                var hasFinance = ResolveFinancePermission(query);
                var warnings = new List<string>(retrieval.Warnings ?? Array.Empty<string>());
                var matches = new List<KnowledgeSearchMatch>();
                var filteredFinancial = 0;

                foreach (var hit in retrieval.Hits)
                {
                    if (IsFinanciallyRestricted(hit.Item) && !hasFinance)
                    {
                        filteredFinancial++;
                        continue;
                    }

                    if (!ContainsAllTokens(hit.Item, tokens))
                    {
                        continue;
                    }

                    var score = hit.Score + KnowledgeQueryNormalizer.ExactPhraseBonus(hit.Item, normalized);
                    matches.Add(MapHit(hit, score));
                }

                if (filteredFinancial > 0)
                {
                    warnings.Add(RbacFilteredWarning);
                }

                // Deterministic order: Relevance desc, then Code asc, then Id asc
                var ordered = matches
                    .OrderByDescending(m => m.Relevance)
                    .ThenBy(m => m.Code, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
                    .Skip(offset)
                    .Take(limit)
                    .ToList();

                var groups = BuildGroups(ordered);
                sw.Stop();

                if (ordered.Count == 0)
                {
                    return new KnowledgeSearchResponse
                    {
                        Query = raw.Trim(),
                        NormalizedQuery = normalized,
                        Tokens = tokens,
                        State = KnowledgeSearchUiState.NoResults,
                        Message = NoResultsMessage,
                        Results = ordered,
                        Groups = groups,
                        TotalCount = 0,
                        IndexedItemCount = retrieval.IndexedItemCount,
                        DurationMs = sw.ElapsedMilliseconds,
                        Provider = "PRIMOX_DETERMINISTIC_SEARCH",
                        Warnings = warnings
                    };
                }

                return new KnowledgeSearchResponse
                {
                    Query = raw.Trim(),
                    NormalizedQuery = normalized,
                    Tokens = tokens,
                    State = KnowledgeSearchUiState.Found,
                    Message = $"{ordered.Count} resultado(s) encontrado(s).",
                    Results = ordered,
                    Groups = groups,
                    TotalCount = ordered.Count,
                    IndexedItemCount = retrieval.IndexedItemCount,
                    DurationMs = sw.ElapsedMilliseconds,
                    Provider = "PRIMOX_DETERMINISTIC_SEARCH",
                    Warnings = warnings
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger?.LogWarning($"KnowledgeSearchService error: {ex.Message}");
                return new KnowledgeSearchResponse
                {
                    Query = raw.Trim(),
                    NormalizedQuery = normalized,
                    Tokens = tokens,
                    State = KnowledgeSearchUiState.Error,
                    Message = ErrorMessage,
                    DurationMs = sw.ElapsedMilliseconds,
                    ErrorDetail = ex.GetType().Name, // never dump stack to UI
                    Warnings = new[] { "SEARCH_EXCEPTION" }
                };
            }
        }

        /// <summary>
        /// Direct SourceId access with RBAC — unauthorized financial sources return null.
        /// </summary>
        public KnowledgeSearchMatch? GetBySourceId(string sourceType, string sourceId, bool hasFinancePermission)
        {
            if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(sourceId))
            {
                return null;
            }

            var item = _retrieval.Index.SnapshotItems()
                .FirstOrDefault(i =>
                    string.Equals(i.SourceEntity, sourceType, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(i.SourceEntityId, sourceId, StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                return null;
            }

            if (IsFinanciallyRestricted(item) && !hasFinancePermission)
            {
                return null;
            }

            return MapItem(item, relevance: 0, matchedFields: Array.Empty<string>(), excerpt: item.Symptom);
        }

        public static bool IsFinanciallyRestricted(KnowledgeItem item)
        {
            if (item == null) return false;
            if (item.Type == KnowledgeType.PURCHASE) return true;
            if (string.Equals(item.Classification, "FINANCIAL", StringComparison.OrdinalIgnoreCase)) return true;
            if (item.SourceEntity.Contains("Purchase", StringComparison.OrdinalIgnoreCase)) return true;
            if (item.SourceEntity.Contains("Financeiro", StringComparison.OrdinalIgnoreCase)) return true;
            if (item.SourceEntity.Contains("Conta", StringComparison.OrdinalIgnoreCase) &&
                item.SourceEntity.Contains("Receber", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool ResolveFinancePermission(KnowledgeSearchQuery query)
        {
            if (query.HasFinancePermission.HasValue) return query.HasFinancePermission.Value;
            return query.UserContext?.HasFinancePermission ?? false;
        }

        private static string? TryFilter(IReadOnlyDictionary<string, string>? filters, string key)
        {
            if (filters == null) return null;
            return filters.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;
        }

        private static KnowledgeSearchMatch MapHit(KnowledgeSearchHit hit, double score)
        {
            return MapItem(hit.Item, score, hit.MatchedFields, hit.Excerpt, hit.MatchLabel);
        }

        private static KnowledgeSearchMatch MapItem(
            KnowledgeItem item,
            double relevance,
            IReadOnlyList<string> matchedFields,
            string? excerpt,
            string matchLabel = "token-match")
        {
            var evidence = BuildEvidence(item, relevance, excerpt, matchLabel);
            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["code"] = item.Code ?? string.Empty,
                ["system"] = item.System ?? string.Empty,
                ["classification"] = item.Classification ?? "TECHNICAL",
                ["matchedFields"] = string.Join(",", matchedFields ?? Array.Empty<string>())
            };

            return new KnowledgeSearchMatch
            {
                Id = item.ItemId,
                Title = string.IsNullOrWhiteSpace(item.Title) ? (item.Code ?? item.ItemId) : item.Title,
                Summary = !string.IsNullOrWhiteSpace(excerpt)
                    ? excerpt!
                    : (!string.IsNullOrWhiteSpace(item.Symptom) ? item.Symptom : Truncate(item.BodyText, 220)),
                Type = item.Type.ToString(),
                KnowledgeType = item.Type,
                SourceType = item.SourceEntity ?? string.Empty,
                SourceId = item.SourceEntityId ?? item.Code ?? item.ItemId,
                Relevance = relevance,
                Evidence = evidence,
                Metadata = meta,
                GroupKey = KnowledgeSearchGroups.FromType(item.Type),
                Code = item.Code ?? string.Empty,
                Item = item
            };
        }

        private static IReadOnlyList<EvidenceItem> BuildEvidence(
            KnowledgeItem item,
            double relevance,
            string? excerpt,
            string matchLabel)
        {
            // Only real found evidence — no silent inference fill.
            if (string.IsNullOrWhiteSpace(item.Code) && string.IsNullOrWhiteSpace(item.Title))
            {
                return Array.Empty<EvidenceItem>();
            }

            var kind = item.Type switch
            {
                KnowledgeType.TECHNICAL_CASE => EvidenceKind.Knowledge,
                KnowledgeType.DIAGNOSTIC_CASE => EvidenceKind.DiagnosticCase,
                KnowledgeType.PROCEDURE => EvidenceKind.Procedure,
                KnowledgeType.WORK_ORDER => EvidenceKind.WorkOrderNote,
                KnowledgeType.MEASUREMENT => EvidenceKind.Measurement,
                _ => EvidenceKind.Other
            };

            return new[]
            {
                new EvidenceItem
                {
                    Kind = kind,
                    SourceCode = item.Code ?? string.Empty,
                    Title = item.Title ?? string.Empty,
                    Excerpt = excerpt ?? item.Symptom ?? string.Empty,
                    RelevanceScore = relevance,
                    RelevanceLabel = matchLabel,
                    Classification = item.Classification ?? "TECHNICAL"
                }
            };
        }

        private static IReadOnlyList<KnowledgeSearchGroup> BuildGroups(IReadOnlyList<KnowledgeSearchMatch> ordered)
        {
            // Only categories with real results — no empty aesthetic groups.
            var order = new[]
            {
                KnowledgeSearchGroups.Conhecimento,
                KnowledgeSearchGroups.Diagnosticos,
                KnowledgeSearchGroups.Procedimentos,
                KnowledgeSearchGroups.Outros
            };

            var groups = new List<KnowledgeSearchGroup>();
            foreach (var key in order)
            {
                var items = ordered.Where(m => m.GroupKey == key).ToList();
                if (items.Count == 0) continue;
                groups.Add(new KnowledgeSearchGroup
                {
                    Key = key,
                    DisplayName = KnowledgeSearchGroups.DisplayName(key),
                    Items = items
                });
            }

            return groups;
        }

        
        /// <summary>
        /// Multi-token queries require ALL tokens to appear in the item (AND).
        /// Prevents incidental hits (e.g. query "..._NAO_..." matching titles with "nao").
        /// Single-token queries unchanged.
        /// </summary>
        private static bool ContainsAllTokens(KnowledgeItem item, IReadOnlyList<string> tokens)
        {
            if (tokens == null || tokens.Count == 0) return false;
            if (tokens.Count == 1)
            {
                var only = tokens[0];
                var hay1 = DeterministicKnowledgeIndex.RemoveDiacritics(string.Join(' ',
                    item.Code, item.Title, item.Symptom, item.Diagnosis, item.Solution,
                    item.System, item.VehicleModel, item.BodyText,
                    string.Join(' ', item.Tags ?? Array.Empty<string>())).ToLowerInvariant());
                return hay1.Contains(only, StringComparison.Ordinal);
            }

            var hay = DeterministicKnowledgeIndex.RemoveDiacritics(string.Join(' ',
                item.Code, item.Title, item.Symptom, item.Diagnosis, item.Solution,
                item.System, item.VehicleModel, item.BodyText,
                string.Join(' ', item.Tags ?? Array.Empty<string>())).ToLowerInvariant());

            foreach (var token in tokens)
            {
                if (!hay.Contains(token, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
        private static string Truncate(string? text, int max)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return text.Length <= max ? text : text[..(max - 3)] + "...";
        }
    }
}

