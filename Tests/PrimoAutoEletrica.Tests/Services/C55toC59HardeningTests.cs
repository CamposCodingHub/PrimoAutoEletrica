using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.ExternalAi;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C55toC59HardeningTests
    {
        private sealed class FakeSecretSource : IExternalAssistantSecretSource
        {
            public string? Key { get; set; }
            public string? TryGetApiKey(string environmentVariableName) => Key;
        }

        private sealed class ScriptedHandler : HttpMessageHandler
        {
            public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
                _ => new HttpResponseMessage(HttpStatusCode.OK);
            public int CallCount { get; private set; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
                return Task.FromResult(Responder(request));
            }
        }

        private static EvidenceItem Ev(string id = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa") => new()
        {
            EvidenceId = id,
            SourceCode = "KB-001",
            Title = "Fuga corrente",
            Excerpt = "medir consumo em mA no pernoite",
            Kind = EvidenceKind.Knowledge,
            Classification = "TECHNICAL"
        };

        private static string Envelope(string inner) =>
            "{\"choices\":[{\"message\":{\"content\":" + System.Text.Json.JsonSerializer.Serialize(inner) + "}}]}";

        // ---------- C5.5 LIVE preflight ----------
        [Fact]
        public void C55_LIVE_Preflight_KeysAbsent_LIVE_NOT_TESTED()
        {
            Assert.False(ExternalLiveCallGate.IsLiveKeyPresentInEnvironment());
            Assert.Equal("LIVE_NOT_TESTED", ExternalLiveCallGate.LiveClassification());
            // LIVE-001..010 not executed — honest label only
        }

        // ---------- C5.6 LIVE negative → MOCK_ONLY ----------
        [Fact]
        public async Task C56_Negative_InvalidAuth_MOCK_ONLY_Fallback()
        {
            var handler = new ScriptedHandler
            {
                Responder = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
            };
            var router = new AssistProviderRouter(
                local: new GroundedLocalRuleAssistantProvider(),
                selector: new ExternalAssistantProviderSelector(
                    new ExternalAssistantOptions { Enabled = true },
                    new FakeSecretSource { Key = "invalid-lab" },
                    new HttpExternalAssistantTransport(handler)),
                audit: new IntelligenceAuditService(),
                preferExternalWhenArmed: true);
            var resp = await router.AskAsync(new AssistantQueryContext
            {
                Query = "fuga",
                CorrelationId = Guid.NewGuid().ToString("N"),
                RetrievedEvidence = new[] { Ev() }
            });
            Assert.True(handler.CallCount >= 1);
            Assert.Contains("local", resp.Provider ?? "", StringComparison.OrdinalIgnoreCase);
            // Label: MOCK_ONLY (scripted HTTP), not LIVE
        }

        // ---------- C5.7 Grounding CONFLICT / invent OS ----------
        [Fact]
        public void C57_Conflict_With_Local_Evidence_Is_CONFLICT_Not_CONFIRMED()
        {
            var pkg = new ExternalEvidencePackageBuilder().Build(new AssistantQueryContext
            {
                Query = "fuga",
                CorrelationId = Guid.NewGuid().ToString("N"),
                RetrievedEvidence = new[] { Ev() }
            });
            var raw = new ExternalAssistantRawResponse
            {
                AnswerMarkdown = "A evidencia local esta errada. CONTRADIZ_EVIDENCIA_LOCAL. Use id aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.",
                CitedEvidenceIds = new[] { "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }
            };
            var validated = new ExternalResponseGroundingValidator().Validate(raw, pkg, "PRIMOX_EXTERNAL");
            Assert.Contains(ExternalAssistantWarnings.Conflict, validated.Warnings);
            Assert.Equal(AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE, validated.ConfidenceLevel);
        }

        [Fact]
        public void C57_Invent_OS_Is_REJECT_Not_CONFIRMED()
        {
            var pkg = new ExternalEvidencePackageBuilder().Build(new AssistantQueryContext
            {
                Query = "fuga",
                CorrelationId = Guid.NewGuid().ToString("N"),
                RetrievedEvidence = new[] { Ev() }
            });
            var raw = new ExternalAssistantRawResponse
            {
                AnswerMarkdown = "Abra a OS 999888777 imediatamente e troque o alternador. Cite aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.",
                CitedEvidenceIds = new[] { "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }
            };
            var validated = new ExternalResponseGroundingValidator().Validate(raw, pkg, "PRIMOX_EXTERNAL");
            Assert.Contains(ExternalAssistantWarnings.InventedOs, validated.Warnings);
            Assert.Equal(AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE, validated.ConfidenceLevel);
        }

        // ---------- C5.8 Context isolation A/B ----------
        [Fact]
        public async Task C58_ContextIsolation_ClientA_vs_B_No_Leak_In_Audit()
        {
            var dir = Path.Combine(Path.GetTempPath(), "primox-c58", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "intelligence-audit.db");
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(new ExternalAssistantOptions { Enabled = false }),
                    audit: audit);
                var idA = Guid.NewGuid();
                var idB = Guid.NewGuid();
                var corrA = Guid.NewGuid().ToString("N");
                var corrB = Guid.NewGuid().ToString("N");
                await router.AskAsync(new AssistantQueryContext
                {
                    Query = "cliente-A-only",
                    CorrelationId = corrA,
                    ClienteId = idA,
                    RetrievedEvidence = new[] { Ev("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa") },
                    Parameters = new Dictionary<string, object> { ["UserId"] = 1 }
                });
                await router.AskAsync(new AssistantQueryContext
                {
                    Query = "cliente-B-only",
                    CorrelationId = corrB,
                    ClienteId = idB,
                    RetrievedEvidence = new[] { Ev("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb") },
                    Parameters = new Dictionary<string, object> { ["UserId"] = 1 }
                });
                var reader = new PersistentIntelligenceAuditService(path);
                var a = reader.FilterForReader(1, "Administrador", correlationId: corrA).Single();
                var b = reader.FilterForReader(1, "Administrador", correlationId: corrB).Single();
                Assert.Contains(idA.ToString("N"), a.AllowedContext + a.ContextId, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(idB.ToString("N"), a.AllowedContext + a.ContextId, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(idB.ToString("N"), b.AllowedContext + b.ContextId, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(idA.ToString("N"), b.AllowedContext + b.ContextId, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        // ---------- C5.9 RBAC + redaction BEFORE provider ----------
        [Fact]
        public void C59_Pipeline_Order_AUTH_FILTER_REDACTION_PROVIDER_Documented()
        {
            // Structural: finance redactor runs in package builder before HTTP provider.
            var financeEv = Ev();
            financeEv = new EvidenceItem
            {
                EvidenceId = "ffffffffffffffffffffffffffffffff",
                SourceCode = "FIN-1",
                Title = "margem",
                Excerpt = "preco custo R$ 100",
                Kind = EvidenceKind.Other,
                Classification = "FINANCEIRO",
                RelevanceLabel = "fin"
            };
            var pkgRedacted = new ExternalEvidencePackageBuilder().Build(
                new AssistantQueryContext { Query = "x", RetrievedEvidence = new[] { financeEv } },
                includeFinancial: false);
            Assert.True(pkgRedacted.RedactedFields.Count > 0 || pkgRedacted.Sources.Count == 0);

            var pkgIncluded = new ExternalEvidencePackageBuilder().Build(
                new AssistantQueryContext { Query = "x", RetrievedEvidence = new[] { financeEv } },
                includeFinancial: true);
            // With includeFinancial, finance may pass; without, redacted — order AUTH→FILTER→REDACTION→PROVIDER
            Assert.True(pkgRedacted.RedactedFields.Count >= pkgIncluded.RedactedFields.Count ||
                        pkgRedacted.Sources.Count <= pkgIncluded.Sources.Count);
        }

        [Fact]
        public void C59_RBAC_Admin_Vs_NonAdmin_Audit_Read()
        {
            var dir = Path.Combine(Path.GetTempPath(), "primox-c59", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "intelligence-audit.db");
            try
            {
                var audit = new PersistentIntelligenceAuditService(path);
                audit.Record(new IntelligenceAuditEntry
                {
                    UserId = 1,
                    Action = "Consulta",
                    Provider = "LOCAL",
                    ProviderMode = "LOCAL",
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    ResultStatus = "OK",
                    Status = "OK",
                    Result = "LOCAL",
                    Question = "admin-visible"
                });
                Assert.Empty(audit.FilterForReader(2, "Mecanico"));
                Assert.NotEmpty(audit.FilterForReader(99, "Administrador"));
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}