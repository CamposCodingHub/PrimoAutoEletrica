using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// Índice determinístico in-memory (C2.1). Sem embeddings / vetores / OpenAI.
    /// Campos indexados: text, tags, system, model, symptom, code, diagnosis, vehicle.
    /// </summary>
    public sealed class DeterministicKnowledgeIndex
    {
        private readonly object _gate = new();
        private readonly Dictionary<string, KnowledgeItem> _itemsByDedup = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, HashSet<string>> _postings = new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, double> FieldWeights = new(StringComparer.OrdinalIgnoreCase)
        {
            ["code"] = 5.0,
            ["title"] = 3.0,
            ["symptom"] = 3.0,
            ["tags"] = 2.5,
            ["system"] = 2.0,
            ["diagnosis"] = 2.0,
            ["solution"] = 1.5,
            ["model"] = 1.5,
            ["vehicle"] = 1.5,
            ["text"] = 1.0
        };

        public int Count
        {
            get { lock (_gate) return _itemsByDedup.Count; }
        }

        public IReadOnlyList<KnowledgeItem> SnapshotItems()
        {
            lock (_gate)
            {
                return _itemsByDedup.Values.OrderBy(i => i.Code, StringComparer.OrdinalIgnoreCase).ToList();
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _itemsByDedup.Clear();
                _postings.Clear();
            }
        }

        /// <summary>
        /// Upsert por DedupKey — evita duplicatas impróprias; atualiza se a mesma chave voltar.
        /// </summary>
        public bool Upsert(KnowledgeItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (string.IsNullOrWhiteSpace(item.ItemId) && string.IsNullOrWhiteSpace(item.Code))
            {
                throw new ArgumentException("KnowledgeItem requires ItemId or Code.", nameof(item));
            }

            var key = item.DedupKey;
            lock (_gate)
            {
                var isNew = !_itemsByDedup.ContainsKey(key);
                if (!isNew)
                {
                    RemovePostingsUnlocked(key);
                }

                _itemsByDedup[key] = item;
                AddPostingsUnlocked(key, item);
                return isNew;
            }
        }

        public int UpsertMany(IEnumerable<KnowledgeItem> items)
        {
            var added = 0;
            foreach (var item in items)
            {
                if (Upsert(item)) added++;
            }
            return added;
        }

        public KnowledgeSearchResult Search(KnowledgeSearchQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);
            var text = query.Text?.Trim() ?? string.Empty;
            var tokens = Tokenize(text);
            var max = query.MaxResults <= 0 ? 20 : Math.Min(query.MaxResults, 100);

            lock (_gate)
            {
                if (tokens.Count == 0)
                {
                    return new KnowledgeSearchResult
                    {
                        Query = text,
                        Hits = Array.Empty<KnowledgeSearchHit>(),
                        IndexedItemCount = _itemsByDedup.Count,
                        Timestamp = DateTimeOffset.Now,
                        Warnings = new[] { "EMPTY_QUERY" }
                    };
                }

                var scores = new Dictionary<string, (double Score, HashSet<string> Fields)>(StringComparer.OrdinalIgnoreCase);

                foreach (var token in tokens)
                {
                    if (!_postings.TryGetValue(token, out var posting))
                    {
                        continue;
                    }

                    foreach (var postingKey in posting)
                    {
                        // postingKey format: "{dedupKey}|{field}"
                        var sep = postingKey.LastIndexOf('|');
                        if (sep <= 0) continue;
                        var dedup = postingKey[..sep];
                        var field = postingKey[(sep + 1)..];

                        if (!_itemsByDedup.TryGetValue(dedup, out var item))
                        {
                            continue;
                        }

                        if (query.TypeFilter.HasValue && item.Type != query.TypeFilter.Value)
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(query.SystemFilter) &&
                            !string.Equals(item.System, query.SystemFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(query.VehicleModelFilter) &&
                            (item.VehicleModel ?? string.Empty).IndexOf(query.VehicleModelFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        var weight = FieldWeights.TryGetValue(field, out var w) ? w : 1.0;
                        if (!scores.TryGetValue(dedup, out var acc))
                        {
                            acc = (0, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                        }

                        acc.Score += weight;
                        acc.Fields.Add(field);
                        scores[dedup] = acc;
                    }
                }

                var hits = scores
                    .Select(kv =>
                    {
                        var item = _itemsByDedup[kv.Key];
                        return new KnowledgeSearchHit
                        {
                            Item = item,
                            Score = kv.Value.Score,
                            MatchLabel = "token-match",
                            MatchedFields = kv.Value.Fields.OrderBy(f => f).ToList(),
                            Excerpt = BuildExcerpt(item, tokens)
                        };
                    })
                    .OrderByDescending(h => h.Score)
                    .ThenBy(h => h.Item.Code, StringComparer.OrdinalIgnoreCase)
                    .Take(max)
                    .ToList();

                return new KnowledgeSearchResult
                {
                    Query = text,
                    Hits = hits,
                    IndexedItemCount = _itemsByDedup.Count,
                    Timestamp = DateTimeOffset.Now
                };
            }
        }

        private void AddPostingsUnlocked(string dedupKey, KnowledgeItem item)
        {
            IndexField(dedupKey, "code", item.Code);
            IndexField(dedupKey, "title", item.Title);
            IndexField(dedupKey, "symptom", item.Symptom);
            IndexField(dedupKey, "system", item.System);
            IndexField(dedupKey, "diagnosis", item.Diagnosis);
            IndexField(dedupKey, "solution", item.Solution);
            IndexField(dedupKey, "model", item.VehicleModel);
            IndexField(dedupKey, "vehicle", string.Join(' ', new[] { item.VehicleModel, item.VehiclePlate }.Where(s => !string.IsNullOrWhiteSpace(s))));
            IndexField(dedupKey, "tags", string.Join(' ', item.Tags ?? Array.Empty<string>()));
            IndexField(dedupKey, "text", item.BodyText);
        }

        private void RemovePostingsUnlocked(string dedupKey)
        {
            var prefix = dedupKey + "|";
            var toRemove = new List<(string Token, string Posting)>();
            foreach (var kv in _postings)
            {
                foreach (var p in kv.Value)
                {
                    if (p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        toRemove.Add((kv.Key, p));
                    }
                }
            }

            foreach (var (token, posting) in toRemove)
            {
                if (_postings.TryGetValue(token, out var set))
                {
                    set.Remove(posting);
                    if (set.Count == 0) _postings.Remove(token);
                }
            }
        }

        private void IndexField(string dedupKey, string field, string? value)
        {
            foreach (var token in Tokenize(value))
            {
                if (!_postings.TryGetValue(token, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _postings[token] = set;
                }

                set.Add($"{dedupKey}|{field}");
            }
        }

        public static IReadOnlyList<string> Tokenize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            var normalized = RemoveDiacritics(text.ToLowerInvariant());
            var tokens = new List<string>();
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                }
                else if (sb.Length > 0)
                {
                    AddToken(tokens, sb.ToString());
                    sb.Clear();
                }
            }

            if (sb.Length > 0)
            {
                AddToken(tokens, sb.ToString());
            }

            return tokens.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static void AddToken(List<string> tokens, string token)
        {
            if (token.Length < 2) return;
            // Stopwords curtas em PT
            if (token is "de" or "da" or "do" or "das" or "dos" or "em" or "no" or "na" or "um" or "uma" or "os" or "as" or "ao" or "para" or "com" or "sem" or "por")
            {
                return;
            }

            tokens.Add(token);
        }

        public static string RemoveDiacritics(string text)
        {
            var formD = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var ch in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string BuildExcerpt(KnowledgeItem item, IReadOnlyList<string> tokens)
        {
            var candidates = new[] { item.Symptom, item.Diagnosis, item.Solution, item.Title, item.BodyText };
            foreach (var c in candidates)
            {
                if (string.IsNullOrWhiteSpace(c)) continue;
                var norm = RemoveDiacritics(c.ToLowerInvariant());
                if (tokens.Any(t => norm.Contains(t, StringComparison.Ordinal)))
                {
                    return c.Length <= 220 ? c : c[..217] + "...";
                }
            }

            var fallback = !string.IsNullOrWhiteSpace(item.Symptom) ? item.Symptom : item.Title;
            return fallback.Length <= 220 ? fallback : fallback[..217] + "...";
        }
    }
}
