using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PrimoAutoEletrica.Services.ExternalAi
{
    /// <summary>
    /// C3.15 — inventory and classify outbound payload fields for data minimization.
    /// NECESSÁRIO / DESNECESSÁRIO / PROIBIDO. Never includes secrets.
    /// </summary>
    public sealed class ExternalPayloadFieldClassification
    {
        public string Field { get; init; } = string.Empty;
        public string Classification { get; init; } = string.Empty; // NECESSARIO | DESNECESSARIO | PROIBIDO
        public string Rationale { get; init; } = string.Empty;
        public bool IncludedInDefaultPayload { get; init; }
    }

    public static class ExternalPayloadMinimizationAuditor
    {
        public static IReadOnlyList<ExternalPayloadFieldClassification> Inventory()
        {
            return new[]
            {
                new ExternalPayloadFieldClassification
                {
                    Field = "RequestId",
                    Classification = "NECESSARIO",
                    Rationale = "Correlation / audit without PII",
                    IncludedInDefaultPayload = true
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Query (redacted)",
                    Classification = "NECESSARIO",
                    Rationale = "User technical question after finance redaction",
                    IncludedInDefaultPayload = true
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Model",
                    Classification = "NECESSARIO",
                    Rationale = "Provider model selection (non-secret)",
                    IncludedInDefaultPayload = true
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Evidence[].EvidenceId",
                    Classification = "NECESSARIO",
                    Rationale = "Grounding validator maps claims to local evidence",
                    IncludedInDefaultPayload = true
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Evidence[].SourceType/SourceId/Title/Excerpt",
                    Classification = "NECESSARIO",
                    Rationale = "Technical excerpts only; finance classification stripped",
                    IncludedInDefaultPayload = true
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Instruction (fail-closed system prompt)",
                    Classification = "NECESSARIO",
                    Rationale = "Constrains provider to cite evidence IDs only",
                    IncludedInDefaultPayload = true
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Vehicle technical descriptors (make/model/year/voltage)",
                    Classification = "NECESSARIO",
                    Rationale = "May appear inside evidence excerpts only when retrieved locally",
                    IncludedInDefaultPayload = false
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "WorkOrderId / Knowledge IDs / DiagnosticCase IDs",
                    Classification = "NECESSARIO",
                    Rationale = "As SourceId when present in local package",
                    IncludedInDefaultPayload = true
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Financial amounts / prices / margins / DRE / salaries",
                    Classification = "PROIBIDO",
                    Rationale = "ExternalFinanceRedactor strips by default (includeFinancial=false)",
                    IncludedInDefaultPayload = false
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "API keys / Bearer tokens / passwords",
                    Classification = "PROIBIDO",
                    Rationale = "Only Authorization header at transport; never in body/logs/audit",
                    IncludedInDefaultPayload = false
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Customer PII (CPF/CNPJ/phone/address/email)",
                    Classification = "PROIBIDO",
                    Rationale = "Not part of ExternalEvidencePackage builder fields",
                    IncludedInDefaultPayload = false
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Full database dumps / unrelated client contexts",
                    Classification = "DESNECESSARIO",
                    Rationale = "Context isolation: only retrieved evidence for the query",
                    IncludedInDefaultPayload = false
                },
                new ExternalPayloadFieldClassification
                {
                    Field = "Raw HTTP RawBody echo of secrets",
                    Classification = "PROIBIDO",
                    Rationale = "TruncateSafe redacts Bearer/api_key/sk-",
                    IncludedInDefaultPayload = false
                }
            };
        }

        /// <summary>Concrete list of fields that would be serialized into the OpenAI-compatible body.</summary>
        public static IReadOnlyList<string> ProviderReceives(ExternalAssistantRequestPayload payload)
        {
            ArgumentNullException.ThrowIfNull(payload);
            var list = new List<string>
            {
                "model=" + (payload.Model ?? string.Empty),
                "temperature=0",
                "messages[system]=fail-closed-instruction",
                "messages[user].RequestId=" + payload.RequestId,
                "messages[user].Query=" + Trunc(payload.Query, 80),
                "messages[user].EvidenceCount=" + (payload.Evidence?.Count ?? 0)
            };
            if (payload.Evidence != null)
            {
                foreach (var e in payload.Evidence.Take(20))
                {
                    list.Add($"evidence={{EvidenceId={e.EvidenceId};SourceType={e.SourceType};SourceId={e.SourceId};Title={Trunc(e.Title,40)};Classification={e.Classification}}}");
                }
            }
            return list;
        }

        public static bool DefaultPayloadContainsProibido(ExternalAssistantRequestPayload payload)
        {
            var redactor = new ExternalFinanceRedactor();
            var blob = (payload.Query ?? string.Empty) + " " +
                       string.Join(" ", payload.Evidence?.Select(e => e.Excerpt + " " + e.Title) ?? Array.Empty<string>());
            return redactor.FindFinanceLeaks(blob).Count > 0 ||
                   blob.Contains("sk-", StringComparison.OrdinalIgnoreCase) ||
                   blob.Contains("Bearer ", StringComparison.OrdinalIgnoreCase);
        }

        private static string Trunc(string? s, int n)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }
    }

    /// <summary>
    /// C3.13 honesty gate: live HTTP only when env secret present AND caller sets LIVE_TEST.
    /// Never invents a live result.
    /// </summary>
    public static class ExternalLiveCallGate
    {
        public static bool IsLiveKeyPresentInEnvironment()
        {
            foreach (var name in new[]
                     {
                         ExternalAssistantOptions.DefaultApiKeyEnvironmentVariable,
                         "OPENAI_API_KEY",
                         "PRIMOX_EXTERNAL_AI_KEY"
                     })
            {
                var v = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrWhiteSpace(v)) return true;
            }
            return false;
        }

        public static string LiveClassification()
        {
            return IsLiveKeyPresentInEnvironment()
                ? "LIVE_KEY_PRESENT"
                : "LIVE_NOT_TESTED";
        }
    }
}
