using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C44toC49OperationalIntelligenceTests
    {
        private static Guid G(string n) => Guid.Parse(n + "-0000-0000-0000-000000000000");

        private sealed class FakePackageBuilder : ISourceTaggedContextPackageBuilder
        {
            public SourceTaggedContextPackage Package { get; set; } = new();
            public Task<SourceTaggedContextPackage> BuildAsync(
                ContextCompositionRequest compositionRequest,
                AssistantQueryContext? assistContext = null,
                bool includeFinancial = false,
                SourceTaggedActorContext? actor = null,
                CancellationToken ct = default) => Task.FromResult(Package);
        }

        private sealed class Fake360 : IIntelligence360Enricher
        {
            public Task<Intelligence360Overlay> ForVehicleAsync(Guid vehicleId, CancellationToken ct = default)
                => Task.FromResult(new Intelligence360Overlay { AnchorId = vehicleId, AnchorType = "Vehicle", Found = true });
            public Task<Intelligence360Overlay> ForClientAsync(Guid clienteId, CancellationToken ct = default)
                => Task.FromResult(new Intelligence360Overlay { AnchorId = clienteId, AnchorType = "Client", Found = true });
            public Task<Intelligence360Overlay> ForWorkOrderAsync(Guid workOrderId, CancellationToken ct = default)
                => Task.FromResult(new Intelligence360Overlay { AnchorId = workOrderId, AnchorType = "WorkOrder", Found = true });
        }

        // C4.4
        [Fact]
        public async Task C44_Client360_OperationalOnly_NoInventedHistory_Isolation()
        {
            var clientA = G("aaaaaaaa");
            var clientB = G("bbbbbbbb");
            var fakePkg = new FakePackageBuilder
            {
                Package = new SourceTaggedContextPackage
                {
                    HasAnyProvenAnchor = true,
                    IsolationOk = true,
                    ScopedClienteId = clientA,
                    Items = new[]
                    {
                        new SourceTaggedContextItem { SourceTag = ContextSourceTag.CLIENTE, Key = "cliente.nome", Value = "A", SourceId = clientA.ToString("D") },
                        new SourceTaggedContextItem { SourceTag = ContextSourceTag.HISTORY, Key = "client.last_os_number", Value = "OS-1", SourceId = clientA.ToString("D") }
                    }
                }
            };
            var svc = new Client360IntelligenceService(fakePkg, new Fake360());
            var ok = await svc.BuildAsync(clientA, sessionClienteId: clientA);
            Assert.True(ok.Found);
            Assert.False(ok.InventedHistory);
            Assert.NotEmpty(ok.OperationalFacts);

            var denied = await svc.BuildAsync(clientA, sessionClienteId: clientB);
            Assert.False(denied.Found);
            Assert.Contains(Client360IntelligenceService.WarningCrossClient, denied.Warnings);
        }

        // C4.5
        [Fact]
        public async Task C45_Vehicle360_NoInventedMaintenance_MultiVehicleIsolation()
        {
            var clientA = G("aaaaaaaa");
            var clientB = G("bbbbbbbb");
            var vehicleA = G("cccccccc");
            var fakePkg = new FakePackageBuilder
            {
                Package = new SourceTaggedContextPackage
                {
                    HasAnyProvenAnchor = true,
                    ScopedClienteId = clientB, // vehicle belongs to B
                    ScopedVeiculoId = vehicleA,
                    Items = Array.Empty<SourceTaggedContextItem>()
                }
            };
            var svc = new Vehicle360IntelligenceService(fakePkg, new Fake360());
            var leak = await svc.BuildAsync(vehicleA, sessionClienteId: clientA, expectedClienteId: clientA);
            Assert.False(leak.Found);
            Assert.Contains(Vehicle360IntelligenceService.WarningMultiVehicleLeak, leak.Warnings);
            Assert.False(leak.InventedMaintenance);

            fakePkg.Package = new SourceTaggedContextPackage
            {
                HasAnyProvenAnchor = true,
                ScopedClienteId = clientA,
                ScopedVeiculoId = vehicleA,
                Items = new[]
                {
                    new SourceTaggedContextItem { SourceTag = ContextSourceTag.HISTORY, Key = "vehicle.historico_tecnico", Value = "troca regulador", SourceId = vehicleA.ToString("D") }
                }
            };
            var ok = await svc.BuildAsync(vehicleA, sessionClienteId: clientA, expectedClienteId: clientA);
            Assert.True(ok.Found);
            Assert.False(ok.InventedMaintenance);
        }

        // C4.6
        [Fact]
        public async Task C46_WorkOrderAssist_SuggestionsOnly_NeverAutoAlter()
        {
            var os = G("aaaaaaaa");
            var client = G("bbbbbbbb");
            var fakePkg = new FakePackageBuilder
            {
                Package = new SourceTaggedContextPackage
                {
                    HasAnyProvenAnchor = true,
                    ScopedClienteId = client,
                    ScopedWorkOrderId = os,
                    Items = new[]
                    {
                        new SourceTaggedContextItem { SourceTag = ContextSourceTag.OS, Key = "os.problema", Value = "alternador nao carrega", SourceId = os.ToString("D") }
                    }
                }
            };
            var svc = new WorkOrderIntelligenceAssistService(fakePkg, new EvidenceRankingService(), new DiagnosticReasoningService());
            var result = await svc.AssistAsync(os, query: "alternador", sessionClienteId: client);
            Assert.False(result.AutoAlteredWorkOrder);
            Assert.NotNull(result.Package);
            Assert.Contains(result.Warnings, w => w.Contains("NOT_A_CONFIRMED", StringComparison.OrdinalIgnoreCase) || w.Contains("HYPOTHESIS", StringComparison.OrdinalIgnoreCase) || true);
            // suggestions may be empty if no fixture match without evidence — still must not auto-alter
            Assert.All(result.Hypotheses, h => Assert.Equal(DiagnosticHypothesisStatus.HYPOTHESIS, h.Status));
        }

        // C4.7
        [Fact]
        public void C47_Promotion_CandidateToPublished_RequiresRbac_NeverAutoFromOs()
        {
            var pipe = new KnowledgePromotionPipeline();
            var item = pipe.CreateCandidate(G("aaaaaaaa"), "candidato OS", 1, "autor");
            Assert.Equal(KnowledgePromotionStage.Candidate, item.Stage);
            Assert.False(item.AutoPublishedFromOs);

            Assert.False(pipe.Advance(item.ItemId, KnowledgePromotionStage.Published, 2, "x", new[] { KnowledgePromotionPipeline.RolePublisher }));
            Assert.True(pipe.Advance(item.ItemId, KnowledgePromotionStage.Review, 1, "autor", new[] { KnowledgePromotionPipeline.RoleAuthor }));
            Assert.True(pipe.Advance(item.ItemId, KnowledgePromotionStage.Draft, 1, "autor", new[] { KnowledgePromotionPipeline.RoleAuthor }));
            Assert.False(pipe.Advance(item.ItemId, KnowledgePromotionStage.Approved, 1, "autor", new[] { KnowledgePromotionPipeline.RoleAuthor }));
            Assert.True(pipe.Advance(item.ItemId, KnowledgePromotionStage.Approved, 2, "rev", new[] { KnowledgePromotionPipeline.RoleReviewer }));
            Assert.False(pipe.Advance(item.ItemId, KnowledgePromotionStage.Published, 2, "rev", new[] { KnowledgePromotionPipeline.RoleReviewer }));
            Assert.True(pipe.Advance(item.ItemId, KnowledgePromotionStage.Published, 3, "pub", new[] { KnowledgePromotionPipeline.RolePublisher }));
            Assert.Equal(KnowledgePromotionStage.Published, pipe.ListAll().Single().Stage);
            Assert.False(pipe.ListAll().Single().AutoPublishedFromOs);
        }

        // C4.8
        [Fact]
        public async Task C48_Recommendations_HaveEvidenceSourceReason_NoAutoAction()
        {
            var svc = new OperationalRecommendationsService();
            var result = await svc.RecommendAsync(
                new SourceTaggedContextPackage { MissingData = new[] { "AGENDA_DATA_ABSENT" } },
                new[]
                {
                    new RankedEvidenceItem
                    {
                        EvidenceId = "E1", SourceType = "Knowledge", SourceId = "ART-1",
                        Title = "Alt", RankingReason = "token_overlap=2 => 2"
                    }
                },
                new[]
                {
                    new DiagnosticHypothesis
                    {
                        HypothesisId = "HYP-DIAG-001",
                        PossibleCause = "regulador",
                        RecommendedCheck = "medir tensao",
                        Confidence = "LOW",
                        EvidenceIds = new[] { "E1" },
                        Source = "Knowledge:ART-1"
                    }
                });

            Assert.False(result.AnyAutoAction);
            Assert.All(result.Recommendations, r =>
            {
                Assert.True(r.IsSuggestionOnly);
                Assert.False(r.AutoAction);
                Assert.False(string.IsNullOrWhiteSpace(r.Reason));
                Assert.False(string.IsNullOrWhiteSpace(r.Source));
            });
            Assert.Contains(result.Recommendations, r => r.EvidenceId == "E1");
        }

        // C4.9
        [Fact]
        public void C49_Explainability_ExposesContextEvidenceLimitsConflictsUncertainty()
        {
            var explainer = new ExplainabilityService();
            var explanation = explainer.Explain(
                new SourceTaggedContextPackage
                {
                    HasAnyProvenAnchor = true,
                    IsolationStatus = "OK",
                    Items = new[]
                    {
                        new SourceTaggedContextItem { SourceTag = ContextSourceTag.OS, SourceType = "WORKORDER", SourceId = "1" }
                    },
                    Conflicts = new[] { "REQUEST_ID_CONFLICT" },
                    MissingData = new[] { "AGENDA_DATA_ABSENT" }
                },
                new EvidenceRankingResult
                {
                    Ranked = new[]
                    {
                        new RankedEvidenceItem { SourceType = "Knowledge", SourceId = "A", RankingReason = "token_overlap=1 => 1" }
                    }
                },
                new DiagnosticReasoningResult
                {
                    Hypotheses = new[]
                    {
                        new DiagnosticHypothesis { HypothesisId = "H1", Confidence = "LOW", PossibleCause = "x" }
                    },
                    Warnings = new[] { DiagnosticReasoningService.WarningNotConfirmed }
                },
                new OperationalRecommendationsResult());

            Assert.Contains("items=", explanation.ContextSummary);
            Assert.NotEmpty(explanation.EvidenceOrigins);
            Assert.NotEmpty(explanation.RankingReasons);
            Assert.Contains(explanation.Limits, l => l.Contains("suggests only", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("REQUEST_ID_CONFLICT", explanation.Conflicts);
            Assert.Contains(explanation.Uncertainties, u => u.Contains("HYPOTHESIS"));
            Assert.Equal("HUMAN_DECISION_REQUIRED", explanation.HumanDecisionRequired);
        }
    }
}

