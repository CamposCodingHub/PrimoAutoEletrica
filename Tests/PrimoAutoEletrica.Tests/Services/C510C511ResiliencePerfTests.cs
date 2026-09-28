using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.ExternalAi;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C510C511ResiliencePerfTests
    {
        private sealed class FakeSecretSource : IExternalAssistantSecretSource
        {
            public string? Key { get; set; }
            public string? TryGetApiKey(string environmentVariableName) => Key;
        }

        private sealed class CountingHandler : HttpMessageHandler
        {
            public int CallCount;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref CallCount);
                throw new HttpRequestException("fail");
            }
        }

        [Fact]
        public async Task C510_Failure_Falls_Back_No_Crash_No_False_Success()
        {
            var handler = new CountingHandler();
            var router = new AssistProviderRouter(
                local: new GroundedLocalRuleAssistantProvider(),
                selector: new ExternalAssistantProviderSelector(
                    new ExternalAssistantOptions { Enabled = true, RequestTimeout = TimeSpan.FromSeconds(5) },
                    new FakeSecretSource { Key = "lab" },
                    new HttpExternalAssistantTransport(handler)),
                audit: new IntelligenceAuditService(),
                preferExternalWhenArmed: true);
            var resp = await router.AskAsync(new AssistantQueryContext
            {
                Query = "resilience",
                CorrelationId = Guid.NewGuid().ToString("N"),
                RetrievedEvidence = new[]
                {
                    new EvidenceItem
                    {
                        EvidenceId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                        SourceCode = "KB-1",
                        Title = "t",
                        Excerpt = "e",
                        Kind = EvidenceKind.Knowledge
                    }
                }
            });
            Assert.NotNull(resp);
            Assert.False(string.IsNullOrWhiteSpace(resp.AnswerMarkdown));
            // Finite: single transport attempt path (no infinite retry loop in router)
            Assert.True(handler.CallCount >= 1 && handler.CallCount <= 3);
            Assert.Contains("local", resp.Provider ?? "", StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void C510_Timeout_Configured_Finite()
        {
            var opt = new ExternalAssistantOptions();
            Assert.True(opt.RequestTimeout > TimeSpan.Zero);
            Assert.True(opt.RequestTimeout <= TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task C511_LOCAL_Perf_Observation_Only_No_Invented_P95()
        {
            var sw = Stopwatch.StartNew();
            var samples = new List<long>();
            var router = new AssistProviderRouter(
                local: new GroundedLocalRuleAssistantProvider(),
                selector: new ExternalAssistantProviderSelector(new ExternalAssistantOptions { Enabled = false }),
                audit: new IntelligenceAuditService());
            for (int i = 0; i < 5; i++)
            {
                var t0 = Stopwatch.GetTimestamp();
                await router.AskAsync(new AssistantQueryContext
                {
                    Query = "perf local " + i,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    RetrievedEvidence = new[]
                    {
                        new EvidenceItem
                        {
                            EvidenceId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                            SourceCode = "KB-1",
                            Title = "t",
                            Excerpt = "e",
                            Kind = EvidenceKind.Knowledge
                        }
                    }
                });
                samples.Add((Stopwatch.GetTimestamp() - t0) * 1000 / Stopwatch.Frequency);
            }
            sw.Stop();
            Assert.Equal(5, samples.Count);
            // OBSERVATION_ONLY — n=5 insufficient for p95 claim
            Assert.True(samples.Average() >= 0);
            // Explicit: no p95 invented
        }
    }
}