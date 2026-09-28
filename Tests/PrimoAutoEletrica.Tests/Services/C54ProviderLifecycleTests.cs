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
    /// <summary>C5.4 — Provider lifecycle matrix; never silent switch without audit record.</summary>
    public sealed class C54ProviderLifecycleTests
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

        private static EvidenceItem Ev() => new()
        {
            EvidenceId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            SourceCode = "KB-001",
            Title = "Fuga",
            Excerpt = "medir mA",
            Kind = EvidenceKind.Knowledge,
            Classification = "TECHNICAL"
        };

        private static AssistantQueryContext Ctx(string corr) => new()
        {
            Query = "fuga de corrente",
            CorrelationId = corr,
            RetrievedEvidence = new[] { Ev() },
            Parameters = new Dictionary<string, object> { ["UserId"] = 8 }
        };

        private static string Envelope(string inner) =>
            "{\"choices\":[{\"message\":{\"content\":" + System.Text.Json.JsonSerializer.Serialize(inner) + "}}]}";

        private static string TempDb()
        {
            var dir = Path.Combine(Path.GetTempPath(), "primox-c54", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "intelligence-audit.db");
        }

        private static void TryDelete(string p)
        {
            try { var d = Path.GetDirectoryName(p); if (d != null && Directory.Exists(d)) Directory.Delete(d, true); } catch { }
        }

        [Theory]
        [InlineData(false, null, false, AssistProviderMode.LOCAL)]           // OFF
        [InlineData(true, null, false, AssistProviderMode.LOCAL)]            // ON-no-key
        [InlineData(true, "k", true, AssistProviderMode.DISABLED)]           // kill-switch
        [InlineData(true, "k", false, AssistProviderMode.EXTERNAL)]          // armed candidate
        public void C54_Arming_Priority_Matrix(bool enabled, string? key, bool kill, AssistProviderMode expected)
        {
            var selector = new ExternalAssistantProviderSelector(
                new ExternalAssistantOptions { Enabled = enabled, KillSwitch = kill },
                new FakeSecretSource { Key = key });
            Assert.Equal(expected, ProviderLifecycleResolver.ResolveArmingMode(selector));
        }

        [Fact]
        public async Task C54_OFF_Uses_LOCAL_And_Records()
        {
            var path = TempDb();
            try
            {
                var corr = Guid.NewGuid().ToString("N");
                var audit = new PersistentIntelligenceAuditService(path);
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(new ExternalAssistantOptions { Enabled = false }),
                    audit: audit,
                    preferExternalWhenArmed: true);
                await router.AskAsync(Ctx(corr));
                var row = new PersistentIntelligenceAuditService(path).FilterForReader(8, "Administrador", correlationId: corr).Single();
                Assert.Equal("LOCAL", row.ProviderMode);
                Assert.False(row.FallbackUsed);
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public async Task C54_ON_NoKey_LOCAL_Not_Silent_External()
        {
            var path = TempDb();
            try
            {
                var corr = Guid.NewGuid().ToString("N");
                var audit = new PersistentIntelligenceAuditService(path);
                var handler = new ScriptedHandler();
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(
                        new ExternalAssistantOptions { Enabled = true },
                        new FakeSecretSource { Key = null },
                        new HttpExternalAssistantTransport(handler)),
                    audit: audit,
                    preferExternalWhenArmed: true);
                await router.AskAsync(Ctx(corr));
                Assert.Equal(0, handler.CallCount);
                var row = new PersistentIntelligenceAuditService(path).FilterForReader(8, "Administrador", correlationId: corr).Single();
                Assert.Equal("LOCAL", row.ProviderMode);
            }
            finally { TryDelete(path); }
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)] // invalid auth ~ 4xx
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.InternalServerError)] // 5xx
        [InlineData(HttpStatusCode.BadGateway)]
        public async Task C54_HttpError_Fallback_Recorded(HttpStatusCode code)
        {
            var path = TempDb();
            try
            {
                var corr = Guid.NewGuid().ToString("N");
                var audit = new PersistentIntelligenceAuditService(path);
                var handler = new ScriptedHandler
                {
                    Responder = _ => new HttpResponseMessage(code) { Content = new StringContent("err") }
                };
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(
                        new ExternalAssistantOptions { Enabled = true },
                        new FakeSecretSource { Key = "lab-key" },
                        new HttpExternalAssistantTransport(handler)),
                    audit: audit,
                    preferExternalWhenArmed: true);
                var resp = await router.AskAsync(Ctx(corr));
                Assert.True(handler.CallCount >= 1);
                var row = new PersistentIntelligenceAuditService(path).FilterForReader(8, "Administrador", correlationId: corr).First();
                Assert.Equal("FALLBACK", row.ProviderMode);
                Assert.True(row.FallbackUsed);
                Assert.Contains("local", resp.Provider ?? "", StringComparison.OrdinalIgnoreCase);
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public async Task C54_Malformed_Fallback_Recorded()
        {
            var path = TempDb();
            try
            {
                var corr = Guid.NewGuid().ToString("N");
                var audit = new PersistentIntelligenceAuditService(path);
                var handler = new ScriptedHandler
                {
                    Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{not-json-at-all", Encoding.UTF8, "application/json")
                    }
                };
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(
                        new ExternalAssistantOptions { Enabled = true },
                        new FakeSecretSource { Key = "lab-key" },
                        new HttpExternalAssistantTransport(handler)),
                    audit: audit,
                    preferExternalWhenArmed: true);
                await router.AskAsync(Ctx(corr));
                var row = new PersistentIntelligenceAuditService(path).FilterForReader(8, "Administrador", correlationId: corr).First();
                Assert.Equal("FALLBACK", row.ProviderMode);
                Assert.True(row.FallbackUsed);
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public async Task C54_NetworkError_Fallback_Recorded()
        {
            var path = TempDb();
            try
            {
                var corr = Guid.NewGuid().ToString("N");
                var audit = new PersistentIntelligenceAuditService(path);
                var handler = new ScriptedHandler
                {
                    Responder = _ => throw new HttpRequestException("simulated network down")
                };
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(
                        new ExternalAssistantOptions { Enabled = true },
                        new FakeSecretSource { Key = "lab-key" },
                        new HttpExternalAssistantTransport(handler)),
                    audit: audit,
                    preferExternalWhenArmed: true);
                await router.AskAsync(Ctx(corr));
                var row = new PersistentIntelligenceAuditService(path).FilterForReader(8, "Administrador", correlationId: corr).First();
                Assert.Equal("FALLBACK", row.ProviderMode);
                Assert.True(row.FallbackUsed);
                Assert.False(string.IsNullOrWhiteSpace(row.FailureReason));
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public async Task C54_Valid_Mock_External_Recorded_As_EXTERNAL()
        {
            var path = TempDb();
            try
            {
                var corr = Guid.NewGuid().ToString("N");
                var audit = new PersistentIntelligenceAuditService(path);
                var inner = "{\"answer\":\"ok grounded\",\"citedEvidenceIds\":[\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"],\"suggestedActions\":[\"medir\"],\"missingInformation\":[],\"limitations\":\"lab\"}";
                var handler = new ScriptedHandler
                {
                    Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(Envelope(inner), Encoding.UTF8, "application/json")
                    }
                };
                var router = new AssistProviderRouter(
                    local: new GroundedLocalRuleAssistantProvider(),
                    selector: new ExternalAssistantProviderSelector(
                        new ExternalAssistantOptions { Enabled = true },
                        new FakeSecretSource { Key = "lab-key" },
                        new HttpExternalAssistantTransport(handler)),
                    audit: audit,
                    preferExternalWhenArmed: true);
                var resp = await router.AskAsync(Ctx(corr));
                Assert.True(handler.CallCount >= 1);
                var row = new PersistentIntelligenceAuditService(path).FilterForReader(8, "Administrador", correlationId: corr).First();
                Assert.Equal("EXTERNAL", row.ProviderMode);
                Assert.False(row.FallbackUsed);
                Assert.Equal(corr, resp.CorrelationId);
            }
            finally { TryDelete(path); }
        }

        [Fact]
        public void C54_Timeout_Cell_Documented_As_FALLBACK_Outcome()
        {
            // Outcome mapping: timeout treated like failure → FALLBACK when local available (router path).
            Assert.Equal(AssistProviderMode.FALLBACK, ProviderLifecycleResolver.ResolveOutcomeMode("LOCAL_FALLBACK", "FALLBACK"));
            Assert.Equal("FALLBACK", ProviderLifecycleResolver.ToAuditMode(AssistProviderMode.FALLBACK));
        }
    }
}