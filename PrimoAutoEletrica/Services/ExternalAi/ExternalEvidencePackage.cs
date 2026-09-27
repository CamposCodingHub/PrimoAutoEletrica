using System;
using System.Collections.Generic;
using System.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// Traceable local evidence package sent (or refused) to an external provider.
    /// SourceType/SourceId must map to local retrieval - never invent.
    /// </summary>
    public sealed class ExternalEvidenceSource
    {
        public string EvidenceId { get; init; } = string.Empty;
        public string SourceType { get; init; } = string.Empty;
        public string SourceId { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Excerpt { get; init; } = string.Empty;
        public string Classification { get; init; } = "TECHNICAL";
        public string? RelevanceLabel { get; init; }
    }

    public sealed class ExternalEvidencePackage
    {
        public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
        public string Query { get; init; } = string.Empty;
        public IReadOnlyList<ExternalEvidenceSource> Sources { get; init; } = Array.Empty<ExternalEvidenceSource>();
        public IReadOnlyList<string> RedactedFields { get; init; } = Array.Empty<string>();
        public bool HasEvidence => Sources != null && Sources.Count > 0;
        public IReadOnlyList<string> AllowedEvidenceIds =>
            Sources?.Select(s => s.EvidenceId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            ?? (IReadOnlyList<string>)Array.Empty<string>();
    }

    /// <summary>Wire DTO for outbound HTTP (no secrets, no finance by default).</summary>
    public sealed class ExternalAssistantRequestPayload
    {
        public string RequestId { get; init; } = string.Empty;
        public string Query { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public IReadOnlyList<ExternalEvidenceSource> Evidence { get; init; } = Array.Empty<ExternalEvidenceSource>();
        public string Instruction { get; init; } =
            "Answer ONLY using the provided evidence IDs. Cite EvidenceId for every claim. Never invent sources, prices, approvals, purchases, fiscal actions, or cross-client data. If evidence is insufficient, say so.";
    }

    /// <summary>Parsed provider output before grounding validation.</summary>
    public sealed class ExternalAssistantRawResponse
    {
        public string AnswerMarkdown { get; init; } = string.Empty;
        public IReadOnlyList<string> CitedEvidenceIds { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> SuggestedActions { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> MissingInformation { get; init; } = Array.Empty<string>();
        public string? Limitations { get; init; }
        public string? RawBody { get; init; }
        public int? HttpStatusCode { get; init; }
    }
}