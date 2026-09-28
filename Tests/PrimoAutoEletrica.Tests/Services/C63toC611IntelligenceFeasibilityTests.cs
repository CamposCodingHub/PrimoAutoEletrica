using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.ExternalAi;
using PrimoAutoEletrica.Services.Intelligence;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C64EvaluatorTests
    {
        [Fact]
        public void C64_NonDeterministic_Returns_HUMAN_REVIEW_REQUIRED()
        {
            var c = PrimoxBenchmarkCatalog.All.First(x => !x.DeterministicEvaluable);
            var eval = new BenchmarkEvaluator().Evaluate(c, new AssistantResponse { AnswerMarkdown = "algo" });
            Assert.Equal(EvaluationVerdict.HUMAN_REVIEW_REQUIRED, eval.Overall);
            Assert.Null(eval.DeterministicScore);
        }

        [Fact]
        public void C64_Adversarial_Buy_Refusal_Can_Pass_Safety()
        {
            var c = PrimoxBenchmarkCatalog.All.First(x => x.Domain == "AdversarialPrompt" && x.DeterministicEvaluable);
            var resp = new AssistantResponse
            {
                AnswerMarkdown = c.GoldenAnswer!,
                ConfidenceLevel = AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE,
                RecommendedActions = new[] { "Use fluxos operacionais com RBAC" }
            };
            var eval = new BenchmarkEvaluator().Evaluate(c, resp);
            Assert.NotEqual(EvaluationVerdict.NOT_TESTED, eval.Overall);
            Assert.Contains(eval.Criteria, x => x.Criterion == "safety" && x.Verdict == EvaluationVerdict.PASS);
        }
    }

    public sealed class C65KnowledgeVsModelTests
    {
        [Fact]
        public async Task C65_Rules_Scenarios_Run_Model_Scenarios_STUB_OR_MOCK_Without_Runtime()
        {
            var runner = new KnowledgeVsModelRunner(new GroundedLocalRuleAssistantProvider());
            var bench = PrimoxBenchmarkCatalog.All.First(c => c.Domain == "InsufficientEvidence" && c.DeterministicEvaluable);
            var results = await runner.RunCaseAsync(bench);
            Assert.Equal(7, results.Count);
            Assert.Equal("PASS", results.First(r => r.Scenario == KnowledgeVsModelScenario.A_Rules).Status);
            Assert.Contains(results, r => r.Scenario == KnowledgeVsModelScenario.D_ModelNoPrimoxCtx &&
                                          (r.Status == "STUB_ONLY" || r.Status == "MOCK_ONLY"));
            Assert.All(results.Where(r => r.Status is "STUB_ONLY" or "MOCK_ONLY"),
                r => Assert.Equal(BenchmarkCallOutcome.NotTested, r.Metrics!.Outcome));
        }
    }

    public sealed class C66LocalModelTests
    {
        [Fact]
        public async Task C66_Without_Runtime_Is_ENVIRONMENT_DEPENDENCY_Not_Invented()
        {
            var p = new LocalModelAssistantProvider(new LocalModelOptions { RuntimeAvailable = false });
            Assert.False(p.IsConfigured);
            var r = await p.AskAsync(new AssistantQueryContext { Query = "teste", CorrelationId = "c66" });
            Assert.Contains(r.Warnings, w => w.Contains("ENVIRONMENT_DEPENDENCY"));
            Assert.Equal(AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE, r.ConfidenceLevel);
            Assert.Contains("ENVIRONMENT_DEPENDENCY", p.EnvironmentDependencyReason);
        }

        [Fact]
        public async Task C66_With_Mock_Handler_RuntimeAvailable_Can_Call()
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"local ok\"}}]}")
            });
            var p = new LocalModelAssistantProvider(new LocalModelOptions
            {
                RuntimeAvailable = true,
                Endpoint = "http://127.0.0.1:9/v1/chat/completions",
                ModelId = "test-local"
            }, handler);
            Assert.True(p.IsConfigured);
            var r = await p.AskAsync(new AssistantQueryContext { Query = "queda de tensao" });
            Assert.Contains("local ok", r.AnswerMarkdown);
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _fn;
            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> fn) => _fn = fn;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_fn(request));
        }
    }

    public sealed class C67ExternalAdapterTests
    {
        private sealed class FakeSecrets : IExternalAssistantSecretSource
        {
            public string? Key { get; set; }
            public string? TryGetApiKey(string environmentVariableName) => Key;
        }

        [Fact]
        public async Task C67_NoKey_LIVE_NOT_TESTED_No_Network()
        {
            var p = new ExternalModelAssistantProvider(new ExternalModelAdapterOptions(), new FakeSecrets());
            Assert.False(p.IsConfigured);
            Assert.Equal("LIVE_NOT_TESTED", p.LiveStatus);
            var r = await p.AskAsync(new AssistantQueryContext { Query = "bateria" });
            Assert.Contains(r.Warnings!, w => w == "LIVE_NOT_TESTED");
        }

        [Fact]
        public async Task C67_ForceMock_Contract_Without_Key()
        {
            var p = new ExternalModelAssistantProvider(new ExternalModelAdapterOptions
            {
                ForceMock = true,
                MockResponseMarkdown = "mock-answer-c67"
            }, new FakeSecrets());
            Assert.True(p.IsConfigured);
            Assert.Equal("MOCK_ONLY", p.LiveStatus);
            var r = await p.AskAsync(new AssistantQueryContext { Query = "teste" });
            Assert.Contains("mock-answer-c67", r.AnswerMarkdown);
            Assert.Contains(r.Warnings!, w => w == "EXTERNAL_MOCK_ONLY");
        }

        [Fact]
        public void C67_Cost_With_Unverified_Registry_Is_PRICE_NOT_VERIFIED()
        {
            var reg = new ModelRegistry();
            var model = reg.GetRequired("openai-compatible-external");
            var eng = new IntelligenceCostEngine();
            var est = eng.EstimateMonthly(model, IntelligenceCostEngine.DefaultScenarios[1], workshops: 10, usersPerWorkshop: 3);
            Assert.Equal("PRICE_NOT_VERIFIED", est.Status);
            Assert.Null(est.MonthlyCost);
        }
    }

    public sealed class C68CostEngineTests
    {
        [Fact]
        public void C68_Scenarios_LOW_MEDIUM_HIGH_EXTREME_Declared()
        {
            Assert.Equal(4, IntelligenceCostEngine.DefaultScenarios.Count);
            Assert.Contains(IntelligenceCostEngine.DefaultScenarios, s => s.Kind == UsageScenarioKind.EXTREME);
            Assert.All(IntelligenceCostEngine.DefaultScenarios, s => Assert.False(string.IsNullOrWhiteSpace(s.Hypothesis)));
        }

        [Fact]
        public void C68_Verified_Price_Computes_Theoretical_Monthly()
        {
            var model = new ModelDefinition
            {
                ModelId = "fixture",
                DisplayName = "fixture",
                ProviderFamily = "test",
                ExecutionMode = IntelligenceExecutionMode.ExternalModel,
                InputPricePer1M = 0.15m,
                OutputPricePer1M = 0.60m,
                PriceSource = PriceSourceKind.ManualOperatorEntry,
                PriceCheckedAt = DateTimeOffset.Parse("2026-09-28T00:00:00-03:00")
            };
            var est = new IntelligenceCostEngine().EstimateMonthly(model, IntelligenceCostEngine.DefaultScenarios[0], 1, 1);
            Assert.Equal("PRICE_VERIFIED_CALC", est.Status);
            Assert.NotNull(est.MonthlyCost);
            Assert.True(est.MonthlyCost > 0);
            Assert.Equal("THEORETICAL", est.EvidenceClass);
        }
    }

    public sealed class C69InfraTests
    {
        [Fact]
        public void C69_Default_Profiles_PRICE_NOT_VERIFIED_Theoretical()
        {
            var eco = new LocalInfraEconomics();
            Assert.True(eco.DefaultProfiles.Count >= 2);
            foreach (var p in eco.DefaultProfiles)
            {
                var b = eco.Evaluate(p, workshops: 5, queriesPerMonth: 1000);
                Assert.Equal("PRICE_NOT_VERIFIED", b.Status);
                Assert.Equal(InfraEvidenceClass.THEORETICAL, b.EvidenceClass);
                Assert.Null(b.CostPerQuery);
            }
        }
    }

    public sealed class C610MatrixTests
    {
        [Fact]
        public void C610_Baseline_Matrix_Honest_NOT_TESTED_No_Fake_Scores()
        {
            var cells = new QualityLatencyCostMatrixBuilder().BuildBaselineHonestyMatrix();
            Assert.Equal(7, cells.Count);
            Assert.All(cells, c =>
            {
                Assert.True(c.QualityScore is null || c.QualityStatus != "NOT_TESTED");
                if (c.QualityStatus == "NOT_TESTED") Assert.Null(c.QualityScore);
                Assert.Equal("PRICE_NOT_VERIFIED", c.CostStatus);
            });
        }
    }

    public sealed class C611RouterTests
    {
        [Fact]
        public async Task C611_Blocks_Unsafe_Buy_Intent()
        {
            var router = new IntelligenceRouter(new GroundedLocalRuleAssistantProvider());
            var r = await router.RouteAsync(new AssistantQueryContext { Query = "compre automaticamente peca X" });
            Assert.Equal(IntelligenceRouteDecision.BlockedUnsafeIntent, r.Decision);
            Assert.Contains(r.Response.Warnings!, w => w == "ROUTER_BLOCKED_UNSAFE_INTENT");
        }

        [Fact]
        public async Task C611_Deterministic_First_Then_SafeFallback_When_No_Models()
        {
            var router = new IntelligenceRouter(new GroundedLocalRuleAssistantProvider());
            var r = await router.RouteAsync(new AssistantQueryContext
            {
                Query = "queda de tensao na partida com bateria fraca",
                CorrelationId = "c611"
            });
            Assert.True(r.Decision is IntelligenceRouteDecision.DeterministicRules or IntelligenceRouteDecision.SafeFallback);
            Assert.Contains(r.Response.Warnings!, w => w == "ROUTER_ASSISTIVE_ONLY");
        }

        [Fact]
        public void C611_IsUnsafeIntent_Detects_Stock_Finance()
        {
            Assert.True(IntelligenceRouter.IsUnsafeIntent("alterar estoque agora"));
            Assert.True(IntelligenceRouter.IsUnsafeIntent("pagar fornecedor"));
            Assert.False(IntelligenceRouter.IsUnsafeIntent("como medir queda de tensao?"));
        }
    }
}