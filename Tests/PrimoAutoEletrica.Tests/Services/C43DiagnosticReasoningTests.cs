using System;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C43DiagnosticReasoningTests
    {
        [Fact]
        public void FixtureCases_DIAG001_to_DIAG008_Present()
        {
            var svc = new DiagnosticReasoningService();
            var cases = svc.GetFixtureCases();
            Assert.Equal(8, cases.Count);
            Assert.Contains(cases, c => c.CaseId == "DIAG-001");
            Assert.Contains(cases, c => c.CaseId == "DIAG-008");
        }

        [Fact]
        public async Task Reason_ProducesHypothesis_NotConfirmedDiagnosis()
        {
            var ranking = new EvidenceRankingService();
            var ranked = await ranking.RankAsync(new EvidenceRankingRequest
            {
                Query = "alternador nao carrega",
                Candidates = new[]
                {
                    new PrimoAutoEletrica.Models.EvidenceItem
                    {
                        EvidenceId = "E-ALT",
                        Kind = PrimoAutoEletrica.Models.EvidenceKind.Knowledge,
                        SourceCode = "ART-ALT",
                        Title = "Alternador nao carrega",
                        Excerpt = "regulador e escovas",
                        Classification = "TECHNICAL"
                    }
                }
            });

            var svc = new DiagnosticReasoningService(ranking: ranking);
            var result = await svc.ReasonAsync(new DiagnosticReasoningRequest
            {
                CaseId = "DIAG-001",
                Symptom = "alternador nao carrega",
                RankedEvidence = ranked.Ranked
            });

            Assert.False(result.IsConfirmedDiagnosis);
            Assert.Contains(DiagnosticReasoningService.WarningNotConfirmed, result.Warnings);
            Assert.NotEmpty(result.Hypotheses);
            Assert.All(result.Hypotheses, h =>
            {
                Assert.Equal(DiagnosticHypothesisStatus.HYPOTHESIS, h.Status);
                Assert.Contains("HYPOTHESIS", h.Disclaimer, StringComparison.OrdinalIgnoreCase);
                Assert.False(string.IsNullOrWhiteSpace(h.PossibleCause));
                Assert.False(string.IsNullOrWhiteSpace(h.RecommendedCheck));
                Assert.False(string.IsNullOrWhiteSpace(h.Confidence));
                Assert.False(string.IsNullOrWhiteSpace(h.Source));
            });
        }

        [Fact]
        public async Task Reason_AllFixtureCases_ReturnHypothesisShape()
        {
            var svc = new DiagnosticReasoningService();
            foreach (var fx in svc.GetFixtureCases())
            {
                var result = await svc.ReasonAsync(new DiagnosticReasoningRequest
                {
                    CaseId = fx.CaseId,
                    Symptom = fx.Symptom,
                    RankedEvidence = Array.Empty<RankedEvidenceItem>()
                });
                Assert.False(result.IsConfirmedDiagnosis);
                Assert.NotEmpty(result.Hypotheses);
                Assert.Contains(result.Hypotheses, h => h.PossibleCause.Contains(fx.ExpectedCauseHint.Split(' ').First(), StringComparison.OrdinalIgnoreCase) || h.HypothesisId.Contains(fx.CaseId));
                Assert.All(result.Hypotheses, h => Assert.Equal(DiagnosticHypothesisStatus.HYPOTHESIS, h.Status));
            }
        }

        [Fact]
        public async Task Reason_CrossClient_FailClosed()
        {
            var svc = new DiagnosticReasoningService();
            var result = await svc.ReasonAsync(new DiagnosticReasoningRequest
            {
                CaseId = "DIAG-001",
                Symptom = "alternador",
                ClienteId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000000"),
                SessionClienteId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000000")
            });
            Assert.Empty(result.Hypotheses);
            Assert.Contains(DiagnosticReasoningService.WarningCrossClient, result.Warnings);
        }

        [Fact]
        public async Task Reason_MissingSymptom_NoInvention()
        {
            var svc = new DiagnosticReasoningService();
            var result = await svc.ReasonAsync(new DiagnosticReasoningRequest());
            Assert.Empty(result.Hypotheses);
            Assert.Contains(DiagnosticReasoningService.WarningNoSymptom, result.Warnings);
        }
    }
}
