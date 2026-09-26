using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Knowledge
{
    /// <summary>
    /// C2.2 query normalization + tokenization documentation.
    /// Deterministic — no LLM / embeddings.
    ///
    /// Normalize: trim, case-insensitive (lower), collapse spaces, strip diacritics when applicable.
    /// Preserve technical tokens: D01–D17, 12V, 24V, CAN, ABS, ECU (kept as alphanumeric tokens len≥2).
    ///
    /// Stop words (PT, documented): de, da, do, das, dos, em, no, na, um, uma, os, as, ao, para, com, sem, por.
    /// Technical tokens are NOT stop words.
    /// </summary>
    public static class KnowledgeQueryNormalizer
    {
        public static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "de", "da", "do", "das", "dos", "em", "no", "na", "um", "uma",
            "os", "as", "ao", "para", "com", "sem", "por"
        };

        public static readonly HashSet<string> PreservedTechnicalTokens = new(StringComparer.OrdinalIgnoreCase)
        {
            "d01","d02","d03","d04","d05","d06","d07","d08","d09","d10",
            "d11","d12","d13","d14","d15","d16","d17",
            "12v","24v","can","abs","ecu"
        };

        public static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var trimmed = text.Trim();
            var collapsed = Regex.Replace(trimmed, @"\s+", " ");
            var lower = collapsed.ToLowerInvariant();
            return DeterministicKnowledgeIndex.RemoveDiacritics(lower);
        }

        public static IReadOnlyList<string> Tokenize(string? text)
        {
            // Delegate to index tokenizer (single source of truth) — stop words applied there.
            return DeterministicKnowledgeIndex.Tokenize(text);
        }

        public static bool IsEmptyQuery(string? text) => string.IsNullOrWhiteSpace(text);

        /// <summary>
        /// Ranking field priority (documented for C2.2):
        /// 1) code (exact/tech id) weight 5
        /// 2) title / symptom weight 3
        /// 3) tags weight 2.5
        /// 4) exact phrase bonus (+4 title, +2 body) when normalized query appears as substring
        /// 5) system / diagnosis weight 2
        /// 6) solution / model / vehicle weight 1.5
        /// 7) body text weight 1
        /// Order among equal scores: Code ascending (deterministic).
        /// </summary>
        public static double ExactPhraseBonus(KnowledgeItem item, string normalizedQuery)
        {
            if (string.IsNullOrWhiteSpace(normalizedQuery) || normalizedQuery.Length < 3)
            {
                return 0;
            }

            double bonus = 0;
            var title = DeterministicKnowledgeIndex.RemoveDiacritics((item.Title ?? string.Empty).ToLowerInvariant());
            var tags = DeterministicKnowledgeIndex.RemoveDiacritics(string.Join(' ', item.Tags ?? Array.Empty<string>()).ToLowerInvariant());
            var body = DeterministicKnowledgeIndex.RemoveDiacritics((item.BodyText ?? string.Empty).ToLowerInvariant());
            var symptom = DeterministicKnowledgeIndex.RemoveDiacritics((item.Symptom ?? string.Empty).ToLowerInvariant());

            if (title.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                bonus += 4.0;
            }
            else if (tags.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                bonus += 3.0;
            }
            else if (symptom.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                bonus += 2.5;
            }
            else if (body.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                bonus += 2.0;
            }

            // Exact code match (D01, KB-..., etc.)
            var code = DeterministicKnowledgeIndex.RemoveDiacritics((item.Code ?? string.Empty).ToLowerInvariant());
            if (!string.IsNullOrEmpty(code) &&
                (string.Equals(code, normalizedQuery, StringComparison.Ordinal) ||
                 normalizedQuery.Equals(code, StringComparison.OrdinalIgnoreCase)))
            {
                bonus += 6.0;
            }

            return bonus;
        }
    }
}

