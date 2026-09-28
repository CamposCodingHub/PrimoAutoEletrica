using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services.Intelligence
{
    /// <summary>C6.1 — how an intelligence call was executed (vendor-neutral).</summary>
    public enum IntelligenceExecutionMode
    {
        GroundedLocalRule = 0,
        LocalModel = 1,
        ExternalModel = 2,
        Disabled = 3,
        Future = 4,
        KnowledgeOnly = 5,
        Mock = 6,
        Stub = 7
    }

    /// <summary>C6.1 — grounding outcome for benchmark records (not a LIVE claim).</summary>
    public enum BenchmarkGroundingStatus
    {
        NOT_APPLICABLE = 0,
        GROUNDED = 1,
        UNGROUNDED = 2,
        CONFLICT = 3,
        INSUFFICIENT_EVIDENCE = 4,
        NOT_EVALUATED = 5
    }

    /// <summary>C6.1 — call outcome for benchmarking.</summary>
    public enum BenchmarkCallOutcome
    {
        Success = 0,
        Failure = 1,
        Blocked = 2,
        Skipped = 3,
        NotTested = 4
    }

    /// <summary>
    /// C6.1 Model Provider Contract — metrics captured for architecture feasibility.
    /// Never stores secrets or full prompts by default.
    /// </summary>
    public sealed class ProviderBenchmarkRecord
    {
        public string ProviderId { get; init; } = string.Empty;
        public string? ModelId { get; init; }
        public string? ModelVersion { get; init; }
        public IntelligenceExecutionMode ExecutionMode { get; init; }
        public int? PromptTokens { get; init; }
        public int? CompletionTokens { get; init; }
        public int? TotalTokens { get; init; }
        /// <summary>End-to-end latency in milliseconds. Null = NOT_MEASURED.</summary>
        public double? LatencyMs { get; init; }
        /// <summary>Time-to-first-token ms. Null = NOT_MEASURED (common for non-streaming).</summary>
        public double? TtftMs { get; init; }
        public BenchmarkCallOutcome Outcome { get; init; } = BenchmarkCallOutcome.NotTested;
        public string? FailureReason { get; init; }
        public BenchmarkGroundingStatus GroundingStatus { get; init; } = BenchmarkGroundingStatus.NOT_EVALUATED;
        public int EvidenceCount { get; init; }
        /// <summary>Optional quality score 0..1 only when evaluated; null = NOT_TESTED / HUMAN_REVIEW_REQUIRED.</summary>
        public double? QualityScore { get; init; }
        public string? QualityLabel { get; init; }
        public IReadOnlyDictionary<string, string> Metadata { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Estimated cost in currency units. Null + PRICE_NOT_VERIFIED metadata when unverified.</summary>
        public decimal? EstimatedCost { get; init; }
        public string? CostStatus { get; init; }
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
        public string? CorrelationId { get; init; }
        /// <summary>Hash or truncated fingerprint of prompt — never the full prompt by default.</summary>
        public string? PromptFingerprint { get; init; }
        public bool IncludesFullPrompt { get; init; }
        public bool IncludesSecrets { get; init; }

        public static ProviderBenchmarkRecord NotTested(string providerId, IntelligenceExecutionMode mode, string? correlationId = null) =>
            new()
            {
                ProviderId = providerId,
                ExecutionMode = mode,
                Outcome = BenchmarkCallOutcome.NotTested,
                GroundingStatus = BenchmarkGroundingStatus.NOT_EVALUATED,
                CostStatus = "PRICE_NOT_VERIFIED",
                CorrelationId = correlationId,
                Timestamp = DateTimeOffset.UtcNow,
                IncludesFullPrompt = false,
                IncludesSecrets = false
            };
    }
}