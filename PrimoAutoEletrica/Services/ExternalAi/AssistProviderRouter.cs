using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Knowledge;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C3.6 — Assist routing: Local grounded vs External when allowed;
    /// fallback to local on external failure; optional IntelligenceAudit.
    /// </summary>
    public sealed class AssistProviderRouter : IAssistantProvider
    {
        private readonly IAssistantProvider _local;
        private readonly ExternalAssistantProviderSelector _selector;
        private readonly IIntelligenceAuditService? _audit;
        private readonly bool _preferExternalWhenArmed;
        private readonly bool _includeFinancial;
        private readonly string? _modelId;

        public AssistProviderRouter(
            IAssistantProvider? local = null,
            ExternalAssistantProviderSelector? selector = null,
            IIntelligenceAuditService? audit = null,
            bool preferExternalWhenArmed = false,
            bool includeFinancial = false,
            string? modelId = null)
        {
            _local = local ?? new GroundedLocalRuleAssistantProvider();
            _selector = selector ?? new ExternalAssistantProviderSelector();
            _audit = audit;
            _preferExternalWhenArmed = preferExternalWhenArmed;
            _includeFinancial = includeFinancial;
            _modelId = modelId;
        }

        public string ProviderId => "PRIMOX_ASSIST_ROUTER";
        public string DisplayName => "PRIMOX Assist Router (local default / external optional)";
        public bool IsConfigured => true;

        public IAssistantProvider LocalProvider => _local;
        public ExternalAssistantProviderSelector Selector => _selector;

        public async Task<AssistantResponse> AskAsync(AssistantQueryContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(context);

            var correlationId = !string.IsNullOrWhiteSpace(context.CorrelationId)
                ? context.CorrelationId.Trim()
                : (TryParam(context, "CorrelationId") ?? Guid.NewGuid().ToString("N"));
            if (context.Parameters != null && !context.Parameters.ContainsKey("CorrelationId"))
                context.Parameters["CorrelationId"] = correlationId;

            var useExternal = _preferExternalWhenArmed && _selector.WouldArmLiveGates();
            if (!useExternal)
            {
                var local = await _local.AskAsync(context, cancellationToken).ConfigureAwait(false);
                Record(context, local, "LOCAL", null, null);
                return StampOrigin(local, "local", correlationId);
            }

            IAssistantProvider external;
            try
            {
                external = _selector.CreateExternalProvider(includeFinancial: _includeFinancial);
            }
            catch (Exception ex)
            {
                var fallback = await _local.AskAsync(context, cancellationToken).ConfigureAwait(false);
                var withWarn = AppendWarning(fallback, ExternalAssistantWarnings.FallbackLocal);
                Record(context, withWarn, "LOCAL_FALLBACK", ex.GetType().Name, "selector-exception");
                return StampOrigin(withWarn, "local-fallback", correlationId);
            }

            try
            {
                var response = await external.AskAsync(context, cancellationToken).ConfigureAwait(false);

                var shouldFallback =
                    response.ConfidenceLevel == AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE &&
                    response.Warnings != null &&
                    (Contains(response.Warnings, ExternalAssistantWarnings.NetworkError) ||
                     Contains(response.Warnings, ExternalAssistantWarnings.Timeout) ||
                     Contains(response.Warnings, ExternalAssistantWarnings.AuthFailed) ||
                     Contains(response.Warnings, ExternalAssistantWarnings.MalformedResponse));

                if (shouldFallback)
                {
                    var fallback = await _local.AskAsync(context, cancellationToken).ConfigureAwait(false);
                    var merged = AppendWarning(fallback, ExternalAssistantWarnings.FallbackLocal);
                    Record(context, merged, "LOCAL_FALLBACK", string.Join(",", response.Warnings), "external-fail-closed");
                    return StampOrigin(merged, "local-fallback", correlationId);
                }

                var reject = Contains(response.Warnings ?? Array.Empty<string>(), ExternalAssistantWarnings.Ungrounded)
                    ? "EXTERNAL_UNGROUNDED"
                    : null;
                Record(context, response, "EXTERNAL", null, reject);
                return StampOrigin(response, "external", correlationId);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var fallback = await _local.AskAsync(context, cancellationToken).ConfigureAwait(false);
                var withWarn = AppendWarning(fallback, ExternalAssistantWarnings.FallbackLocal);
                Record(context, withWarn, "LOCAL_FALLBACK", HttpExternalAssistantProvider.SanitizeException(ex), "exception");
                return StampOrigin(withWarn, "local-fallback", correlationId);
            }
        }

        private void Record(AssistantQueryContext context, AssistantResponse response, string result, string? failure, string? rejectReason)
        {
            if (_audit == null) return;

            var evidenceIds = response.Evidence == null
                ? string.Empty
                : string.Join(",", response.Evidence.Select(e => e.EvidenceId).Where(id => !string.IsNullOrWhiteSpace(id)));

            var allowedCtx = BuildAllowedContext(context);
            var status = !string.IsNullOrWhiteSpace(rejectReason) ? "REJECTED"
                : !string.IsNullOrWhiteSpace(failure) ? "FALLBACK"
                : "OK";

            var correlationId = !string.IsNullOrWhiteSpace(context.CorrelationId) ? context.CorrelationId.Trim() : (TryParam(context, "CorrelationId") ?? Guid.NewGuid().ToString("N"));
            var sessionId = TryParam(context, "SessionId");
            var contextType = InferContextType(context);
            var contextId = InferContextId(context);
            var grounding = InferGroundingStatus(response, rejectReason);
            var providerMode = InferProviderMode(result, status);
            var security = InferSecurityDecision(status, rejectReason);
            var fallbackUsed = string.Equals(result, "LOCAL_FALLBACK", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(status, "FALLBACK", StringComparison.OrdinalIgnoreCase);
            var action = !string.IsNullOrWhiteSpace(rejectReason) ? "Reject"
                : fallbackUsed ? "Fallback"
                : string.Equals(result, "EXTERNAL", StringComparison.OrdinalIgnoreCase) ? "ExternalConsulta"
                : "Consulta";

            _audit.Record(new IntelligenceAuditEntry
            {
                Question = context.Query ?? string.Empty,
                UserName = TryParam(context, "UserName") ?? string.Empty,
                UserId = context.Parameters != null && context.Parameters.TryGetValue("UserId", out var uid) && int.TryParse(uid?.ToString(), out var parsedUid) ? parsedUid : null,
                SessionId = sessionId,
                Action = action,
                Provider = response.Provider ?? ProviderId,
                ProviderMode = providerMode,
                ContextType = contextType,
                ContextId = contextId,
                CorrelationId = correlationId,
                EvidenceCount = response.Evidence?.Count ?? 0,
                EvidenceIds = evidenceIds,
                GroundingStatus = grounding,
                ResultStatus = status,
                FailureReason = failure ?? rejectReason,
                DurationMs = TryLongParam(context, "DurationMs"),
                FallbackUsed = fallbackUsed,
                SecurityDecision = security,
                Model = _modelId ?? _selector.Options.ModelId ?? string.Empty,
                Result = result,
                Status = status,
                Failure = failure,
                RejectReason = rejectReason,
                Error = failure,
                AnswerSummary = (response.AnswerMarkdown ?? string.Empty).Length > 200
                    ? response.AnswerMarkdown!.Substring(0, 200)
                    : response.AnswerMarkdown,
                EvidenceSummary = $"evidence={response.Evidence?.Count ?? 0}; warnings={string.Join('|', response.Warnings ?? Array.Empty<string>())}",
                AllowedContext = allowedCtx,
                ContextSummary = allowedCtx,
                MissingEvidence = response.MissingInformation == null ? null : string.Join("; ", response.MissingInformation)
            });
        }

        private static string? TryParam(AssistantQueryContext context, string key)
        {
            if (context.Parameters == null) return null;
            if (!context.Parameters.TryGetValue(key, out var v) || v == null) return null;
            var s = v.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        private static long TryLongParam(AssistantQueryContext context, string key)
        {
            var s = TryParam(context, key);
            return long.TryParse(s, out var n) && n >= 0 ? n : 0;
        }

        private static string InferContextType(AssistantQueryContext context)
        {
            if (context.WorkOrder != null && !string.IsNullOrWhiteSpace(context.WorkOrder.Number)) return "OS";
            if (context.Vehicle != null) return "VEHICLE";
            if (context.ClienteId.HasValue) return "CLIENT";
            if (context.RetrievedEvidence != null && context.RetrievedEvidence.Count > 0) return "COMPOSITE";
            return "NONE";
        }

        private static string InferContextId(AssistantQueryContext context)
        {
            if (context.WorkOrder != null && !string.IsNullOrWhiteSpace(context.WorkOrder.Number))
                return "OS:" + context.WorkOrder.Number;
            if (context.Vehicle != null && !string.IsNullOrWhiteSpace(context.Vehicle.Plate))
                return "VEHICLE:" + context.Vehicle.Plate;
            if (context.ClienteId.HasValue)
                return "CLIENT:" + context.ClienteId.Value.ToString("N");
            return string.Empty;
        }

        private static string InferGroundingStatus(AssistantResponse response, string? rejectReason)
        {
            var warnings = response.Warnings ?? Array.Empty<string>();
            if (Contains(warnings, ExternalAssistantWarnings.Conflict))
                return "CONFLICT";
            if (Contains(warnings, ExternalAssistantWarnings.InventedOs))
                return "REJECT";
            if (string.Equals(rejectReason, "EXTERNAL_UNGROUNDED", StringComparison.OrdinalIgnoreCase))
                return "UNGROUNDED";
            if (!string.IsNullOrWhiteSpace(rejectReason))
                return "REJECT";
            if (Contains(warnings, ExternalAssistantWarnings.Ungrounded))
                return "UNGROUNDED";
            if (response.Evidence != null && response.Evidence.Count > 0)
                return "CONFIRMED";
            return "N_A";
        }

        private static string InferProviderMode(string result, string status)
            => ProviderLifecycleResolver.ToAuditMode(ProviderLifecycleResolver.ResolveOutcomeMode(result, status));

        private static string InferSecurityDecision(string status, string? rejectReason)
        {
            if (string.Equals(status, "REJECTED", StringComparison.OrdinalIgnoreCase)) return "DENY";
            if (!string.IsNullOrWhiteSpace(rejectReason)) return "DENY";
            return "ALLOW";
        }

        private static string BuildAllowedContext(AssistantQueryContext context)
        {
            var parts = new List<string>();
            if (context.ClienteId.HasValue) parts.Add("ClienteId=" + context.ClienteId.Value.ToString("N"));
            if (context.Vehicle != null)
            {
                if (!string.IsNullOrWhiteSpace(context.Vehicle.Plate)) parts.Add("VehiclePlate=" + context.Vehicle.Plate);
                if (!string.IsNullOrWhiteSpace(context.Vehicle.Make)) parts.Add("VehicleMake=" + context.Vehicle.Make);
                if (!string.IsNullOrWhiteSpace(context.Vehicle.Model)) parts.Add("VehicleModel=" + context.Vehicle.Model);
            }
            if (context.WorkOrder != null && !string.IsNullOrWhiteSpace(context.WorkOrder.Number))
                parts.Add("WorkOrderNumber=" + context.WorkOrder.Number);
            if (context.RetrievedEvidence != null)
            {
                foreach (var e in context.RetrievedEvidence.Take(20))
                {
                    if (!string.IsNullOrWhiteSpace(e?.EvidenceId))
                        parts.Add("EvidenceId=" + e!.EvidenceId);
                }
            }
            if (context.RetrievedKnowledge != null)
            {
                foreach (var k in context.RetrievedKnowledge.Take(10))
                {
                    if (k != null) parts.Add("KnowledgeId=" + k.KnowledgeId.ToString("N"));
                }
            }
            if (context.RetrievedCases != null)
            {
                foreach (var c in context.RetrievedCases.Take(10))
                {
                    if (c != null) parts.Add("DiagnosticCaseId=" + c.CaseId.ToString("N"));
                }
            }
            return string.Join(";", parts);
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<string> warnings, string code) =>
            warnings != null && System.Linq.Enumerable.Any(warnings, w => string.Equals(w, code, StringComparison.OrdinalIgnoreCase));

        private static AssistantResponse AppendWarning(AssistantResponse source, string warning)
        {
            var warnings = new System.Collections.Generic.List<string>(source.Warnings ?? Array.Empty<string>());
            if (!warnings.Exists(w => string.Equals(w, warning, StringComparison.OrdinalIgnoreCase)))
                warnings.Add(warning);

            return new AssistantResponse
            {
                AnswerMarkdown = source.AnswerMarkdown,
                ConfidenceLevel = source.ConfidenceLevel,
                Hypotheses = source.Hypotheses,
                RecommendedActions = source.RecommendedActions,
                CitedSources = source.CitedSources,
                Evidence = source.Evidence,
                Warnings = warnings,
                MissingInformation = source.MissingInformation,
                Provider = source.Provider,
                Timestamp = source.Timestamp ?? DateTimeOffset.Now,
                Disclaimers = source.Disclaimers
            };
        }

        private static AssistantResponse StampOrigin(AssistantResponse source, string origin, string? correlationId = null)
        {
            var provider = string.IsNullOrWhiteSpace(source.Provider) ? origin : source.Provider;
            if (!provider.Contains(origin, StringComparison.OrdinalIgnoreCase) &&
                (origin == "local-fallback" || origin == "external"))
            {
                provider = provider + "/" + origin;
            }

            return new AssistantResponse
            {
                AnswerMarkdown = source.AnswerMarkdown,
                ConfidenceLevel = source.ConfidenceLevel,
                Hypotheses = source.Hypotheses,
                RecommendedActions = source.RecommendedActions,
                CitedSources = source.CitedSources,
                Evidence = source.Evidence,
                Warnings = source.Warnings,
                MissingInformation = source.MissingInformation,
                Provider = provider,
                Timestamp = source.Timestamp ?? DateTimeOffset.Now,
                Disclaimers = source.Disclaimers,
                CorrelationId = correlationId ?? source.CorrelationId
            };
        }
    }
}

