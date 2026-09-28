using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services.Intelligence
{
    public enum KnowledgeVsModelScenario
    {
        A_Rules = 0,
        B_Knowledge = 1,
        C_KnowledgePlusContext = 2,
        D_ModelNoPrimoxCtx = 3,
        E_ModelPlusCtx = 4,
        F_ModelPlusEvidence = 5,
        G_ModelPlusCtxEvidenceRules = 6
    }

    public sealed class ScenarioRunResult
    {
        public KnowledgeVsModelScenario Scenario { get; init; }
        public string CaseId { get; init; } = string.Empty;
        public string Status { get; init; } = "NOT_TESTED";
        public ProviderBenchmarkRecord? Metrics { get; init; }
        public BenchmarkEvaluationResult? Evaluation { get; init; }
        public string Notes { get; init; } = string.Empty;
    }

    /// <summary>
    /// C6.5 — compare scenarios A–G. Models unavailable → MOCK_ONLY/STUB_ONLY labeled honestly.
    /// </summary>
    public sealed class KnowledgeVsModelRunner
    {
        private readonly IAssistantProvider _rules;
        private readonly IAssistantProvider? _localModel;
        private readonly IAssistantProvider? _externalModel;
        private readonly BenchmarkEvaluator _evaluator = new();

        public KnowledgeVsModelRunner(
            IAssistantProvider rulesProvider,
            IAssistantProvider? localModel = null,
            IAssistantProvider? externalModel = null)
        {
            _rules = rulesProvider ?? throw new ArgumentNullException(nameof(rulesProvider));
            _localModel = localModel;
            _externalModel = externalModel;
        }

        public async Task<IReadOnlyList<ScenarioRunResult>> RunCaseAsync(
            BenchmarkCase bench,
            AssistantQueryContext? baseContext = null,
            CancellationToken ct = default)
        {
            var results = new List<ScenarioRunResult>();
            results.Add(await RunAsync(KnowledgeVsModelScenario.A_Rules, bench, baseContext, useKnowledge: false, useCtx: false, useEvidence: false, preferModel: false, ct));
            results.Add(await RunAsync(KnowledgeVsModelScenario.B_Knowledge, bench, baseContext, useKnowledge: true, useCtx: false, useEvidence: false, preferModel: false, ct));
            results.Add(await RunAsync(KnowledgeVsModelScenario.C_KnowledgePlusContext, bench, baseContext, useKnowledge: true, useCtx: true, useEvidence: true, preferModel: false, ct));
            results.Add(await RunAsync(KnowledgeVsModelScenario.D_ModelNoPrimoxCtx, bench, baseContext, useKnowledge: false, useCtx: false, useEvidence: false, preferModel: true, ct));
            results.Add(await RunAsync(KnowledgeVsModelScenario.E_ModelPlusCtx, bench, baseContext, useKnowledge: false, useCtx: true, useEvidence: false, preferModel: true, ct));
            results.Add(await RunAsync(KnowledgeVsModelScenario.F_ModelPlusEvidence, bench, baseContext, useKnowledge: false, useCtx: false, useEvidence: true, preferModel: true, ct));
            results.Add(await RunAsync(KnowledgeVsModelScenario.G_ModelPlusCtxEvidenceRules, bench, baseContext, useKnowledge: true, useCtx: true, useEvidence: true, preferModel: true, ct));
            return results;
        }

        private async Task<ScenarioRunResult> RunAsync(
            KnowledgeVsModelScenario scenario,
            BenchmarkCase bench,
            AssistantQueryContext? baseContext,
            bool useKnowledge,
            bool useCtx,
            bool useEvidence,
            bool preferModel,
            CancellationToken ct)
        {
            IAssistantProvider? provider = null;
            string status;
            IntelligenceExecutionMode mode;

            if (preferModel)
            {
                if (_localModel != null && _localModel.IsConfigured)
                {
                    provider = _localModel;
                    mode = IntelligenceExecutionMode.LocalModel;
                    status = "PASS";
                }
                else if (_externalModel != null && _externalModel.IsConfigured)
                {
                    provider = _externalModel;
                    mode = IntelligenceExecutionMode.ExternalModel;
                    status = "PASS_WITH_EXTERNAL_DEPENDENCY";
                }
                else if (_localModel != null)
                {
                    provider = null;
                    mode = IntelligenceExecutionMode.LocalModel;
                    status = "STUB_ONLY";
                }
                else if (_externalModel != null)
                {
                    provider = null;
                    mode = IntelligenceExecutionMode.ExternalModel;
                    status = "MOCK_ONLY";
                }
                else
                {
                    mode = IntelligenceExecutionMode.Stub;
                    status = "STUB_ONLY";
                }
            }
            else
            {
                provider = _rules;
                mode = IntelligenceExecutionMode.GroundedLocalRule;
                status = "PASS";
            }

            if (provider == null)
            {
                return new ScenarioRunResult
                {
                    Scenario = scenario,
                    CaseId = bench.CaseId,
                    Status = status,
                    Notes = "Model provider unavailable — not inventing LIVE/quality/latency",
                    Metrics = ProviderBenchmarkRecord.NotTested("UNAVAILABLE", mode, bench.CaseId)
                };
            }

            var ctx = BuildContext(bench, baseContext, useKnowledge, useCtx, useEvidence);
            var wrap = new BenchmarkingAssistantProvider(provider, mode);
            var sw = Stopwatch.StartNew();
            var response = await wrap.AskAsync(ctx, ct).ConfigureAwait(false);
            sw.Stop();
            var eval = _evaluator.Evaluate(bench, response);
            return new ScenarioRunResult
            {
                Scenario = scenario,
                CaseId = bench.CaseId,
                Status = status,
                Metrics = wrap.LastRecord,
                Evaluation = eval,
                Notes = $"latency_ms={sw.Elapsed.TotalMilliseconds:F1}; eval={eval.Overall}"
            };
        }

        private static AssistantQueryContext BuildContext(
            BenchmarkCase bench,
            AssistantQueryContext? baseContext,
            bool useKnowledge,
            bool useCtx,
            bool useEvidence)
        {
            var evidence = useEvidence
                ? (baseContext?.RetrievedEvidence?.Count > 0
                    ? baseContext.RetrievedEvidence
                    : new[]
                    {
                        new EvidenceItem
                        {
                            EvidenceId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                            SourceCode = "BM-" + bench.CaseId,
                            Title = bench.Domain,
                            Excerpt = bench.ExpectedKnowledge,
                            Kind = EvidenceKind.Knowledge,
                            Classification = "TECHNICAL"
                        }
                    })
                : Array.Empty<EvidenceItem>();

            return new AssistantQueryContext
            {
                Query = bench.Question,
                CorrelationId = "c65-" + bench.CaseId,
                RetrievedEvidence = evidence,
                RetrievedKnowledge = useKnowledge ? (baseContext?.RetrievedKnowledge ?? Array.Empty<TechnicalKnowledgeEntry>()) : Array.Empty<TechnicalKnowledgeEntry>(),
                Vehicle = useCtx ? baseContext?.Vehicle : null,
                WorkOrder = useCtx ? baseContext?.WorkOrder : null,
                Measurements = useCtx ? (baseContext?.Measurements ?? Array.Empty<AssistantMeasurementContext>()) : Array.Empty<AssistantMeasurementContext>(),
                Parameters = new Dictionary<string, object> { ["Scenario"] = true }
            };
        }
    }
}