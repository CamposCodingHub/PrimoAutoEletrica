using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Intelligence
{
    /// <summary>
    /// C6.1 — wraps any IAssistantProvider and captures ProviderBenchmarkRecord without storing secrets/full prompts.
    /// </summary>
    public sealed class BenchmarkingAssistantProvider : IAssistantProvider
    {
        private readonly IAssistantProvider _inner;
        private readonly IntelligenceExecutionMode _mode;
        private readonly string? _modelId;
        private readonly string? _modelVersion;
        private readonly List<ProviderBenchmarkRecord> _history = new();
        private readonly object _gate = new();

        public BenchmarkingAssistantProvider(
            IAssistantProvider inner,
            IntelligenceExecutionMode mode,
            string? modelId = null,
            string? modelVersion = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _mode = mode;
            _modelId = modelId;
            _modelVersion = modelVersion;
        }

        public string ProviderId => _inner.ProviderId;
        public string DisplayName => _inner.DisplayName + " [benchmark]";
        public bool IsConfigured => _inner.IsConfigured;
        public IAssistantProvider Inner => _inner;

        public IReadOnlyList<ProviderBenchmarkRecord> History
        {
            get { lock (_gate) { return _history.ToArray(); } }
        }

        public ProviderBenchmarkRecord? LastRecord
        {
            get { lock (_gate) { return _history.Count == 0 ? null : _history[^1]; } }
        }

        public async Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            var sw = Stopwatch.StartNew();
            ProviderBenchmarkRecord record;
            try
            {
                var response = await _inner.AskAsync(context, cancellationToken).ConfigureAwait(false);
                sw.Stop();
                var evidenceCount = response.Evidence?.Count
                    ?? response.CitedSources?.Count
                    ?? 0;
                if (evidenceCount == 0 && context.RetrievedEvidence != null)
                    evidenceCount = context.RetrievedEvidence.Count;

                var grounding = MapGrounding(response);
                var failed = response.Warnings != null &&
                             Array.Exists(
                                 System.Linq.Enumerable.ToArray(response.Warnings),
                                 w => w != null && (
                                     w.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
                                     w.Contains("EXTERNAL_NETWORK", StringComparison.OrdinalIgnoreCase) ||
                                     w.Contains("EXTERNAL_AUTH", StringComparison.OrdinalIgnoreCase) ||
                                     w.Contains("EXTERNAL_TIMEOUT", StringComparison.OrdinalIgnoreCase) ||
                                     w.Contains("EXTERNAL_MALFORMED", StringComparison.OrdinalIgnoreCase)));

                record = new ProviderBenchmarkRecord
                {
                    ProviderId = _inner.ProviderId,
                    ModelId = _modelId,
                    ModelVersion = _modelVersion,
                    ExecutionMode = _mode,
                    LatencyMs = sw.Elapsed.TotalMilliseconds,
                    TtftMs = null, // non-streaming path = NOT_MEASURED
                    Outcome = failed ? BenchmarkCallOutcome.Failure : BenchmarkCallOutcome.Success,
                    FailureReason = failed ? string.Join(";", response.Warnings ?? Array.Empty<string>()) : null,
                    GroundingStatus = grounding,
                    EvidenceCount = evidenceCount,
                    QualityScore = null, // C6.4 evaluator fills when run
                    QualityLabel = "NOT_TESTED",
                    EstimatedCost = null,
                    CostStatus = "PRICE_NOT_VERIFIED",
                    Timestamp = DateTimeOffset.UtcNow,
                    CorrelationId = response.CorrelationId ?? context.CorrelationId,
                    PromptFingerprint = Fingerprint(context.Query),
                    IncludesFullPrompt = false,
                    IncludesSecrets = false,
                    Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ConfidenceLevel"] = response.ConfidenceLevel.ToString(),
                        ["HasSufficientEvidence"] = response.HasSufficientEvidence.ToString(),
                        ["TtftStatus"] = "NOT_MEASURED",
                        ["LatencyStatus"] = "MEASURED"
                    }
                };

                lock (_gate) { _history.Add(record); }
                return response;
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                record = new ProviderBenchmarkRecord
                {
                    ProviderId = _inner.ProviderId,
                    ModelId = _modelId,
                    ModelVersion = _modelVersion,
                    ExecutionMode = _mode,
                    LatencyMs = sw.Elapsed.TotalMilliseconds,
                    Outcome = BenchmarkCallOutcome.Blocked,
                    FailureReason = "CANCELLED",
                    GroundingStatus = BenchmarkGroundingStatus.NOT_EVALUATED,
                    EvidenceCount = context.RetrievedEvidence?.Count ?? 0,
                    CostStatus = "PRICE_NOT_VERIFIED",
                    Timestamp = DateTimeOffset.UtcNow,
                    CorrelationId = context.CorrelationId,
                    PromptFingerprint = Fingerprint(context.Query),
                    IncludesFullPrompt = false,
                    IncludesSecrets = false
                };
                lock (_gate) { _history.Add(record); }
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                record = new ProviderBenchmarkRecord
                {
                    ProviderId = _inner.ProviderId,
                    ModelId = _modelId,
                    ModelVersion = _modelVersion,
                    ExecutionMode = _mode,
                    LatencyMs = sw.Elapsed.TotalMilliseconds,
                    Outcome = BenchmarkCallOutcome.Failure,
                    FailureReason = ex.GetType().Name + ":" + Truncate(ex.Message, 120),
                    GroundingStatus = BenchmarkGroundingStatus.NOT_EVALUATED,
                    EvidenceCount = context.RetrievedEvidence?.Count ?? 0,
                    CostStatus = "PRICE_NOT_VERIFIED",
                    Timestamp = DateTimeOffset.UtcNow,
                    CorrelationId = context.CorrelationId,
                    PromptFingerprint = Fingerprint(context.Query),
                    IncludesFullPrompt = false,
                    IncludesSecrets = false
                };
                lock (_gate) { _history.Add(record); }
                throw;
            }
        }

        private static BenchmarkGroundingStatus MapGrounding(AssistantResponse response)
        {
            if (response.ConfidenceLevel == AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE)
                return BenchmarkGroundingStatus.INSUFFICIENT_EVIDENCE;
            if (response.Warnings != null)
            {
                foreach (var w in response.Warnings)
                {
                    if (w == null) continue;
                    if (w.Contains("UNGROUNDED", StringComparison.OrdinalIgnoreCase))
                        return BenchmarkGroundingStatus.UNGROUNDED;
                    if (w.Contains("CONFLICT", StringComparison.OrdinalIgnoreCase))
                        return BenchmarkGroundingStatus.CONFLICT;
                }
            }
            if ((response.Evidence?.Count ?? 0) > 0 || (response.CitedSources?.Count ?? 0) > 0)
                return BenchmarkGroundingStatus.GROUNDED;
            return BenchmarkGroundingStatus.NOT_EVALUATED;
        }

        public static string Fingerprint(string? text)
        {
            if (string.IsNullOrEmpty(text)) return "empty";
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            var sb = new StringBuilder(16);
            for (int i = 0; i < 8; i++)
                sb.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
    }
}