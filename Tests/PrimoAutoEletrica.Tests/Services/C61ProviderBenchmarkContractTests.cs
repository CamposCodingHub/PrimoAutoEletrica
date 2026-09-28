using System;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Intelligence;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>C6.1 — Model Provider Contract for benchmarking metrics.</summary>
    public sealed class C61ProviderBenchmarkContractTests
    {
        [Fact]
        public void C61_NotTested_Factory_Never_Claims_Live_Or_Secrets()
        {
            var r = ProviderBenchmarkRecord.NotTested("PRIMOX_LOCAL_GROUNDED", IntelligenceExecutionMode.GroundedLocalRule, "corr-1");
            Assert.Equal(BenchmarkCallOutcome.NotTested, r.Outcome);
            Assert.Equal("PRICE_NOT_VERIFIED", r.CostStatus);
            Assert.Null(r.EstimatedCost);
            Assert.Null(r.QualityScore);
            Assert.False(r.IncludesFullPrompt);
            Assert.False(r.IncludesSecrets);
            Assert.Equal("corr-1", r.CorrelationId);
        }

        [Fact]
        public void C61_Fingerprint_Is_Stable_And_Not_Full_Prompt()
        {
            var q = "fuga de corrente parasitica no veiculo XYZ com chassis secreto";
            var a = BenchmarkingAssistantProvider.Fingerprint(q);
            var b = BenchmarkingAssistantProvider.Fingerprint(q);
            Assert.Equal(a, b);
            Assert.Equal(16, a.Length);
            Assert.DoesNotContain("fuga", a, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secreto", a, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task C61_Benchmarking_Wrapper_Captures_Latency_Evidence_Correlation_No_Full_Prompt()
        {
            var inner = new GroundedLocalRuleAssistantProvider();
            var wrap = new BenchmarkingAssistantProvider(inner, IntelligenceExecutionMode.GroundedLocalRule, modelId: null, modelVersion: null);
            var corr = Guid.NewGuid().ToString("N");
            var ctx = new AssistantQueryContext
            {
                Query = "queda de tensao na partida com bateria fraca",
                CorrelationId = corr,
                RetrievedEvidence = Array.Empty<EvidenceItem>()
            };

            var response = await wrap.AskAsync(ctx);
            var rec = wrap.LastRecord;
            Assert.NotNull(rec);
            Assert.Equal("PRIMOX_LOCAL_GROUNDED", rec!.ProviderId);
            Assert.Equal(IntelligenceExecutionMode.GroundedLocalRule, rec.ExecutionMode);
            Assert.Equal(BenchmarkCallOutcome.Success, rec.Outcome);
            Assert.NotNull(rec.LatencyMs);
            Assert.True(rec.LatencyMs >= 0);
            Assert.Null(rec.TtftMs); // NOT_MEASURED for non-streaming
            Assert.Equal("NOT_MEASURED", rec.Metadata["TtftStatus"]);
            Assert.Equal(corr, rec.CorrelationId);
            Assert.False(rec.IncludesFullPrompt);
            Assert.False(rec.IncludesSecrets);
            Assert.Equal("PRICE_NOT_VERIFIED", rec.CostStatus);
            Assert.Null(rec.EstimatedCost);
            Assert.Equal("NOT_TESTED", rec.QualityLabel);
            Assert.Null(rec.QualityScore);
            Assert.NotNull(response);
            Assert.Single(wrap.History);
        }

        [Fact]
        public async Task C61_Insufficient_Evidence_Maps_Grounding()
        {
            var wrap = new BenchmarkingAssistantProvider(
                new GroundedLocalRuleAssistantProvider(),
                IntelligenceExecutionMode.GroundedLocalRule);
            await wrap.AskAsync(new AssistantQueryContext
            {
                Query = "xyzzy unexplained anomaly unrelated",
                CorrelationId = "c61-ie"
            });
            Assert.Equal(BenchmarkGroundingStatus.INSUFFICIENT_EVIDENCE, wrap.LastRecord!.GroundingStatus);
        }

        [Fact]
        public void C61_ExecutionModes_Include_Future_Local_External_Disabled()
        {
            var modes = Enum.GetValues<IntelligenceExecutionMode>();
            Assert.Contains(IntelligenceExecutionMode.GroundedLocalRule, modes);
            Assert.Contains(IntelligenceExecutionMode.LocalModel, modes);
            Assert.Contains(IntelligenceExecutionMode.ExternalModel, modes);
            Assert.Contains(IntelligenceExecutionMode.Disabled, modes);
            Assert.Contains(IntelligenceExecutionMode.Future, modes);
        }
    }
}