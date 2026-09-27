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

            var useExternal = _preferExternalWhenArmed && _selector.WouldArmLiveGates();
            if (!useExternal)
            {
                var local = await _local.AskAsync(context, cancellationToken).ConfigureAwait(false);
                Record(context, local, "LOCAL", null, null);
                return StampOrigin(local, "local");
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
                return StampOrigin(withWarn, "local-fallback");
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
                    return StampOrigin(merged, "local-fallback");
                }

                var reject = Contains(response.Warnings ?? Array.Empty<string>(), ExternalAssistantWarnings.Ungrounded)
                    ? "EXTERNAL_UNGROUNDED"
                    : null;
                Record(context, response, "EXTERNAL", null, reject);
                return StampOrigin(response, "external");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var fallback = await _local.AskAsync(context, cancellationToken).ConfigureAwait(false);
                var withWarn = AppendWarning(fallback, ExternalAssistantWarnings.FallbackLocal);
                Record(context, withWarn, "LOCAL_FALLBACK", HttpExternalAssistantProvider.SanitizeException(ex), "exception");
                return StampOrigin(withWarn, "local-fallback");
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

            _audit.Record(new IntelligenceAuditEntry
            {
                Question = context.Query ?? string.Empty,
                UserName = context.Parameters != null && context.Parameters.TryGetValue("UserName", out var un) ? un?.ToString() ?? string.Empty : string.Empty,
                UserId = context.Parameters != null && context.Parameters.TryGetValue("UserId", out var uid) && int.TryParse(uid?.ToString(), out var parsedUid) ? parsedUid : null,
                Provider = response.Provider ?? ProviderId,
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
                EvidenceIds = evidenceIds,
                AllowedContext = allowedCtx,
                ContextSummary = allowedCtx,
                MissingEvidence = response.MissingInformation == null ? null : string.Join("; ", response.MissingInformation)
            });
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

        private static AssistantResponse StampOrigin(AssistantResponse source, string origin)
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
                Disclaimers = source.Disclaimers
            };
        }
    }
}

