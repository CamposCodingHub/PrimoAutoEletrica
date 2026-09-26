using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    /// <summary>
    /// Tipos de item no índice de conhecimento operacional (C2.1).
    /// Camada de retrieval — NÃO substitui entidades de domínio.
    /// </summary>
    public enum KnowledgeType
    {
        TECHNICAL_CASE = 0,
        DIAGNOSTIC_CASE = 1,
        VEHICLE_HISTORY = 2,
        WORK_ORDER = 3,
        CHECKLIST = 4,
        PROCEDURE = 5,
        SYMPTOM = 6,
        CAUSE = 7,
        SOLUTION = 8,
        MEASUREMENT = 9,
        PART = 10,
        PURCHASE = 11,
        OTHER = 12
    }

    /// <summary>
    /// Item indexável unificado. Adapta fontes reais (KB, casos, roteiros D01–D17, OS, etc.).
    /// </summary>
    public sealed class KnowledgeItem
    {
        public string ItemId { get; init; } = string.Empty;
        public KnowledgeType Type { get; init; } = KnowledgeType.OTHER;
        public string Code { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string System { get; init; } = string.Empty;
        public string Symptom { get; init; } = string.Empty;
        public string Diagnosis { get; init; } = string.Empty;
        public string Solution { get; init; } = string.Empty;
        public string VehicleModel { get; init; } = string.Empty;
        public string? VehiclePlate { get; init; }
        public string? WorkOrderNumber { get; init; }
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
        public string BodyText { get; init; } = string.Empty;
        public string SourceEntity { get; init; } = string.Empty;
        public string? SourceEntityId { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }

        /// <summary>Chave estável para deduplicação no índice (Type + Code ou Type + SourceEntityId).</summary>
        public string DedupKey =>
            !string.IsNullOrWhiteSpace(Code)
                ? $"{Type}:{Code.Trim().ToUpperInvariant()}"
                : $"{Type}:{SourceEntityId ?? ItemId}";
    }

    public sealed class KnowledgeSearchQuery
    {
        public string Text { get; init; } = string.Empty;
        public KnowledgeType? TypeFilter { get; init; }
        public string? SystemFilter { get; init; }
        public string? VehicleModelFilter { get; init; }
        public int MaxResults { get; init; } = 20;
    }

    public sealed class KnowledgeSearchHit
    {
        public KnowledgeItem Item { get; init; } = new();
        public double Score { get; init; }
        public string MatchLabel { get; init; } = "token-match";
        public IReadOnlyList<string> MatchedFields { get; init; } = Array.Empty<string>();
        public string Excerpt { get; init; } = string.Empty;
    }

    public sealed class KnowledgeSearchResult
    {
        public string Query { get; init; } = string.Empty;
        public IReadOnlyList<KnowledgeSearchHit> Hits { get; init; } = Array.Empty<KnowledgeSearchHit>();
        public int IndexedItemCount { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
        public string Provider { get; init; } = "PRIMOX_DETERMINISTIC_INDEX";
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Evidência citada na resposta Assist (C2 contract). RelevanceScore = ranking de retrieval,
    /// NÃO probabilidade de falha.
    /// </summary>
    public enum EvidenceKind
    {
        Knowledge = 0,
        DiagnosticCase = 1,
        Measurement = 2,
        WorkOrderNote = 3,
        Procedure = 4,
        Other = 5
    }

    public sealed class EvidenceItem
    {
        public string EvidenceId { get; init; } = Guid.NewGuid().ToString("N");
        public EvidenceKind Kind { get; init; } = EvidenceKind.Other;
        public string SourceCode { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Excerpt { get; init; } = string.Empty;
        /// <summary>Score de ranking de retrieval apenas; null se desconhecido.</summary>
        public double? RelevanceScore { get; init; }
        /// <summary>Ex.: "token-match", "manual-link" — NÃO "fault probability".</summary>
        public string RelevanceLabel { get; init; } = "token-match";
        public AssistantConfidenceLevel ConfidenceContribution { get; init; } = AssistantConfidenceLevel.LOW;
        public string Classification { get; init; } = "TECHNICAL";
    }
}
