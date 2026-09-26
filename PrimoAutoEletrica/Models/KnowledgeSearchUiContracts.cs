using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    /// <summary>
    /// C2.2 — optional stubs for future context (VehicleContext/ClientContext = C2.3, not started).
    /// </summary>
    public sealed class KnowledgeSearchUserContext
    {
        public Guid? VehicleId { get; init; }
        public Guid? WorkOrderId { get; init; }
        public Guid? ClienteId { get; init; }
        public bool HasFinancePermission { get; init; }
    }

    /// <summary>
    /// UI/card DTO for a single deterministic search hit (C2.2).
    /// Relevance = retrieval ranking only — NEVER "confidence" / fault probability.
    /// </summary>
    public sealed class KnowledgeSearchMatch
    {
        public string Id { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Summary { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public KnowledgeType KnowledgeType { get; init; } = KnowledgeType.OTHER;
        public string SourceType { get; init; } = string.Empty;
        public string SourceId { get; init; } = string.Empty;
        /// <summary>Deterministic retrieval relevance score (higher = better match).</summary>
        public double Relevance { get; init; }
        public IReadOnlyList<EvidenceItem> Evidence { get; init; } = Array.Empty<EvidenceItem>();
        public IReadOnlyDictionary<string, string> Metadata { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string GroupKey { get; init; } = "OUTROS";
        public string Code { get; init; } = string.Empty;
        public KnowledgeItem? Item { get; init; }
    }

    public sealed class KnowledgeSearchGroup
    {
        public string Key { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public IReadOnlyList<KnowledgeSearchMatch> Items { get; init; } = Array.Empty<KnowledgeSearchMatch>();
    }

    public enum KnowledgeSearchUiState
    {
        NoQuery = 0,
        Searching = 1,
        NoResults = 2,
        Found = 3,
        Error = 4
    }

    /// <summary>
    /// Full C2.2 search response (normalize → retrieve → rank → group → evidence).
    /// </summary>
    public sealed class KnowledgeSearchResponse
    {
        public string Query { get; init; } = string.Empty;
        public string NormalizedQuery { get; init; } = string.Empty;
        public IReadOnlyList<string> Tokens { get; init; } = Array.Empty<string>();
        public KnowledgeSearchUiState State { get; init; } = KnowledgeSearchUiState.NoQuery;
        public string Message { get; init; } = string.Empty;
        public IReadOnlyList<KnowledgeSearchMatch> Results { get; init; } = Array.Empty<KnowledgeSearchMatch>();
        public IReadOnlyList<KnowledgeSearchGroup> Groups { get; init; } = Array.Empty<KnowledgeSearchGroup>();
        public int TotalCount { get; init; }
        public int IndexedItemCount { get; init; }
        public long DurationMs { get; init; }
        public string Provider { get; init; } = "PRIMOX_DETERMINISTIC_SEARCH";
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public string? ErrorDetail { get; init; }
    }

    public static class KnowledgeSearchGroups
    {
        public const string Conhecimento = "CONHECIMENTO";
        public const string Diagnosticos = "DIAGNOSTICOS";
        public const string Procedimentos = "PROCEDIMENTOS";
        public const string Outros = "OUTROS";

        public static string DisplayName(string key) => key switch
        {
            Conhecimento => "CONHECIMENTO",
            Diagnosticos => "DIAGNÓSTICOS",
            Procedimentos => "PROCEDIMENTOS",
            _ => "OUTROS"
        };

        public static string FromType(KnowledgeType type) => type switch
        {
            KnowledgeType.TECHNICAL_CASE => Conhecimento,
            KnowledgeType.CHECKLIST => Conhecimento,
            KnowledgeType.DIAGNOSTIC_CASE => Diagnosticos,
            KnowledgeType.PROCEDURE => Procedimentos,
            KnowledgeType.SYMPTOM => Procedimentos,
            KnowledgeType.CAUSE => Procedimentos,
            KnowledgeType.SOLUTION => Procedimentos,
            _ => Outros
        };
    }
}
