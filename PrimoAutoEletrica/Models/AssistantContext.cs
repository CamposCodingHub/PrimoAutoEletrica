using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Models
{
    public enum AssistantConfidenceLevel
    {
        INSUFFICIENT_EVIDENCE = 0,
        LOW = 1,
        MEDIUM = 2,
        HIGH = 3
    }

    public sealed class AssistantVehicleContext
    {
        public string? Plate { get; init; }
        public string? Make { get; init; }
        public string? Model { get; init; }
        public int? Year { get; init; }
        public string? Category { get; init; }
        public string Voltage { get; init; } = "12V";
    }

    public sealed class AssistantWorkOrderContext
    {
        public string? Number { get; init; }
        public string? Symptom { get; init; }
        public string? Status { get; init; }
        public IReadOnlyList<string> CurrentItems { get; init; } = Array.Empty<string>();
    }

    public sealed class AssistantMeasurementContext
    {
        public string Parameter { get; init; } = string.Empty;
        public decimal MeasuredValue { get; init; }
        public string Unit { get; init; } = string.Empty;
        public string? ExpectedRange { get; init; }
        public string? Evaluation { get; init; }
    }

    public sealed class AssistantHypothesis
    {
        public string Title { get; init; } = string.Empty;
        public string ProbabilityRating { get; init; } = "Média";
        public string Rationale { get; init; } = string.Empty;
        public IReadOnlyList<string> RequiredVerificationTests { get; init; } = Array.Empty<string>();
    }

    public sealed class AssistantSourceCitation
    {
        public string SourceCode { get; init; } = string.Empty;
        public string SourceTitle { get; init; } = string.Empty;
        public string RelevanceExplanation { get; init; } = string.Empty;
    }

    public sealed class AssistantQueryContext
    {
        public string Query { get; init; } = string.Empty;
        public AssistantVehicleContext? Vehicle { get; init; }
        public AssistantWorkOrderContext? WorkOrder { get; init; }
        public IReadOnlyList<AssistantMeasurementContext> Measurements { get; init; } = Array.Empty<AssistantMeasurementContext>();
        public IReadOnlyList<TechnicalKnowledgeEntry> RetrievedKnowledge { get; init; } = Array.Empty<TechnicalKnowledgeEntry>();
        public IReadOnlyList<DiagnosticCase> RetrievedCases { get; init; } = Array.Empty<DiagnosticCase>();
        /// <summary>C2.1: evidence candidates from deterministic retrieval (preferred over dumping full entities).</summary>
        public IReadOnlyList<EvidenceItem> RetrievedEvidence { get; init; } = Array.Empty<EvidenceItem>();
        /// <summary>Optional authZ scope only — never dump raw PII into prompts.</summary>
        public Guid? ClienteId { get; init; }
        public IReadOnlyList<string> AllowedClasses { get; init; } = Array.Empty<string>();
        public Dictionary<string, object> Parameters { get; init; } = new();
    }

    public sealed class AssistantResponse
    {
        public string AnswerMarkdown { get; init; } = string.Empty;
        /// <summary>C2 contract alias for AnswerMarkdown.</summary>
        public string Answer => AnswerMarkdown;
        public IReadOnlyList<AssistantHypothesis> Hypotheses { get; init; } = Array.Empty<AssistantHypothesis>();
        public IReadOnlyList<string> RecommendedActions { get; init; } = Array.Empty<string>();
        /// <summary>C2 contract alias for RecommendedActions.</summary>
        public IReadOnlyList<string> SuggestedNextSteps => RecommendedActions;
        public IReadOnlyList<AssistantSourceCitation> CitedSources { get; init; } = Array.Empty<AssistantSourceCitation>();
        /// <summary>Additive C2 EvidenceItem list. Empty does not by itself break legacy CitedSources paths.</summary>
        public IReadOnlyList<EvidenceItem> Evidence { get; init; } = Array.Empty<EvidenceItem>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> MissingInformation { get; init; } = Array.Empty<string>();
        public string? Provider { get; init; }
        public DateTimeOffset? Timestamp { get; init; }
        public AssistantConfidenceLevel ConfidenceLevel { get; init; } = AssistantConfidenceLevel.MEDIUM;
        public bool HasSufficientEvidence => ConfidenceLevel != AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE;
        public string Disclaimers { get; init; } = "O PRIMOX Assist atua como copiloto técnico consultivo. O diagnóstico conclusivo e a segurança da operação dependem exclusivamente da validação física do profissional.";
    }

    public interface IAssistantProvider
    {
        string ProviderId { get; }
        string DisplayName { get; }
        bool IsConfigured { get; }
        Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default);
    }
}
