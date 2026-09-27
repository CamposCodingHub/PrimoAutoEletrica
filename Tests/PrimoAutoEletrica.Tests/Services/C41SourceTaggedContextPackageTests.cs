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
    public sealed class C41SourceTaggedContextPackageTests
    {
        private sealed class FakeComposition : IContextCompositionService
        {
            public ComposedIntelligenceContext Result { get; set; } = new();

            public Task<ComposedIntelligenceContext> ComposeAsync(ContextCompositionRequest request, CancellationToken ct = default)
                => Task.FromResult(Result);
        }

        [Fact]
        public void InferTag_FactKeys_MapToClienteVeiculoOs()
        {
            Assert.Equal(ContextSourceTag.CLIENTE, SourceTaggedContextPackageBuilder.InferTagFromFactKey("cliente.nome", "CLIENT"));
            Assert.Equal(ContextSourceTag.VEICULO, SourceTaggedContextPackageBuilder.InferTagFromFactKey("vehicle.placa", "VEHICLE"));
            Assert.Equal(ContextSourceTag.OS, SourceTaggedContextPackageBuilder.InferTagFromFactKey("workorder.status", "WORKORDER"));
            Assert.Equal(ContextSourceTag.KNOWLEDGE, SourceTaggedContextPackageBuilder.InferTagFromFactKey("knowledge.artigo", null));
        }

        [Fact]
        public void InferTag_EvidenceSourceType_MapsKnowledgeAndLocal()
        {
            Assert.Equal(ContextSourceTag.KNOWLEDGE, SourceTaggedContextPackageBuilder.InferTagFromEvidenceSourceType("KnowledgeArticle"));
            Assert.Equal(ContextSourceTag.ASSIST_LOCAL, SourceTaggedContextPackageBuilder.InferTagFromEvidenceSourceType("LocalAssist"));
            Assert.Equal(ContextSourceTag.EXTERNAL_EVIDENCE, SourceTaggedContextPackageBuilder.InferTagFromEvidenceSourceType("Other"));
        }

        [Fact]
        public async Task BuildAsync_TagsFacts_AndSkipsFinancial_WhenNotIncluded()
        {
            var fake = new FakeComposition
            {
                Result = new ComposedIntelligenceContext
                {
                    Query = "teste c4.1",
                    HasAnyProvenAnchor = true,
                    AllProvenFacts = new List<ContextFact>
                    {
                        new() { FactKey = "cliente.nome", Value = "A", SourceEntity = "CLIENT", Classification = "TECHNICAL", Status = ContextProvenanceStatus.PROVEN },
                        new() { FactKey = "vehicle.placa", Value = "ABC1D23", SourceEntity = "VEHICLE", Classification = "TECHNICAL", Status = ContextProvenanceStatus.PROVEN },
                        new() { FactKey = "finance.saldo", Value = "999", SourceEntity = "FINANCE", Classification = "FINANCIAL", Status = ContextProvenanceStatus.PROVEN },
                    }
                }
            };
            var audit = new IntelligenceAuditService();
            var builder = new SourceTaggedContextPackageBuilder(fake, audit: audit);

            var package = await builder.BuildAsync(new ContextCompositionRequest { Query = "teste c4.1" }, includeFinancial: false);

            Assert.Equal(2, package.Items.Count);
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.CLIENTE);
            Assert.Contains(package.Items, i => i.SourceTag == ContextSourceTag.VEICULO);
            Assert.DoesNotContain(package.Items, i => i.Classification.Equals("FINANCIAL", StringComparison.OrdinalIgnoreCase));
            Assert.True(package.HasAnyProvenAnchor);
            Assert.Equal(1, audit.Count);
            Assert.Equal("OK", audit.ListRecent(1)[0].Status);
            Assert.Contains("CLIENTE", audit.ListRecent(1)[0].AllowedContext, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BuildAsync_WithAssistEvidence_AddsKnowledgeTag()
        {
            var fake = new FakeComposition
            {
                Result = new ComposedIntelligenceContext
                {
                    Query = "q",
                    HasAnyProvenAnchor = true,
                    AllProvenFacts = Array.Empty<ContextFact>()
                }
            };
            var assist = new AssistantQueryContext
            {
                Query = "q",
                RetrievedEvidence = new List<EvidenceItem>
                {
                    new()
                    {
                        EvidenceId = "EVID-1",
                        Kind = EvidenceKind.Knowledge,
                        SourceCode = "ART-1",
                        Title = "Boletim",
                        Excerpt = "trecho",
                        Classification = "TECHNICAL"
                    }
                }
            };
            var builder = new SourceTaggedContextPackageBuilder(fake);
            var package = await builder.BuildAsync(new ContextCompositionRequest { Query = "q" }, assistContext: assist);
            Assert.True(package.Items.Count >= 1);
            Assert.Contains(package.Items, i => i.EvidenceId == "EVID-1" || i.SourceTag == ContextSourceTag.KNOWLEDGE || i.SourceTag == ContextSourceTag.EXTERNAL_EVIDENCE);
            Assert.NotNull(package.EvidencePackage);
        }
    }
}