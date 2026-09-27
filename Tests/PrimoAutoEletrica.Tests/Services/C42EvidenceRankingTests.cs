using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class C42EvidenceRankingTests
    {
        private static Guid G(string n) => Guid.Parse(n + "-0000-0000-0000-000000000000");

        [Fact]
        public void Tokenize_IsDeterministicAndOrdered()
        {
            var a = EvidenceRankingService.Tokenize("Alternador 14V falha");
            var b = EvidenceRankingService.Tokenize("Alternador 14V falha");
            Assert.Equal(a, b);
            Assert.Equal(a.OrderBy(x => x, StringComparer.Ordinal).ToList(), a.ToList());
        }

        [Fact]
        public void ScoreComponents_AreExplainable_NoMagic()
        {
            var svc = new EvidenceRankingService();
            var client = G("aaaaaaaa");
            var req = new EvidenceRankingRequest
            {
                Query = "alternador falha",
                ScopedClienteId = client,
                MaxResults = 5,
                Candidates = new[]
                {
                    new EvidenceItem
                    {
                        EvidenceId = "E1",
                        Kind = EvidenceKind.Knowledge,
                        SourceCode = "ART-ALT",
                        Title = "Alternador com falha",
                        Excerpt = "diagnostico de alternador",
                        Classification = "TECHNICAL"
                    }
                }
            };
            var result = svc.RankAsync(req).Result;
            Assert.True(result.AuthorizationOk);
            Assert.Single(result.Ranked);
            var top = result.Ranked[0];
            Assert.Equal("E1", top.EvidenceId);
            Assert.Equal("Knowledge", top.SourceType);
            Assert.Equal("ART-ALT", top.SourceId);
            Assert.False(string.IsNullOrWhiteSpace(top.Title));
            Assert.False(string.IsNullOrWhiteSpace(top.Excerpt));
            Assert.True(top.RelevanceScore > 0);
            Assert.Contains("token_overlap", top.ScoreComponents.Keys);
            Assert.Contains("source_type_boost", top.ScoreComponents.Keys);
            Assert.Contains(EvidenceRankingService.ScoreModelVersion, top.RankingReason);
            Assert.Contains("=>", top.RankingReason);
        }

        [Fact]
        public async Task Rank_IsDeterministic_SameInputSameOrder()
        {
            var svc = new EvidenceRankingService();
            var req = new EvidenceRankingRequest
            {
                Query = "regulador tensao",
                MaxResults = 10,
                Candidates = new[]
                {
                    new EvidenceItem { EvidenceId = "B", Kind = EvidenceKind.Procedure, SourceCode = "D03", Title = "Regulador", Excerpt = "tensao" },
                    new EvidenceItem { EvidenceId = "A", Kind = EvidenceKind.Knowledge, SourceCode = "ART-1", Title = "Regulador de tensao", Excerpt = "teste" },
                    new EvidenceItem { EvidenceId = "C", Kind = EvidenceKind.Other, SourceCode = "X", Title = "outro", Excerpt = "nada" },
                }
            };
            var r1 = await svc.RankAsync(req);
            var r2 = await svc.RankAsync(req);
            Assert.Equal(r1.Ranked.Select(x => x.EvidenceId), r2.Ranked.Select(x => x.EvidenceId));
            Assert.Equal(r1.Ranked.Select(x => x.RelevanceScore), r2.Ranked.Select(x => x.RelevanceScore));
            Assert.True(r1.Deterministic);
        }

        [Fact]
        public async Task CrossClient_Evidence_NeverRetrieved()
        {
            var clientA = G("aaaaaaaa");
            var clientB = G("bbbbbbbb");
            var package = new SourceTaggedContextPackage
            {
                ScopedClienteId = clientA,
                Items = new[]
                {
                    new SourceTaggedContextItem
                    {
                        SourceTag = ContextSourceTag.KNOWLEDGE,
                        EvidenceId = "EV-B",
                        SourceType = "Knowledge",
                        SourceId = "ART-B",
                        Key = "knowledge",
                        Value = "segredo cliente B",
                        Classification = "TECHNICAL",
                        IsolationAnchorId = clientB.ToString("D")
                    },
                    new SourceTaggedContextItem
                    {
                        SourceTag = ContextSourceTag.KNOWLEDGE,
                        EvidenceId = "EV-A",
                        SourceType = "Knowledge",
                        SourceId = "ART-A",
                        Key = "knowledge",
                        Value = "ok cliente A alternador",
                        Classification = "TECHNICAL",
                        IsolationAnchorId = clientA.ToString("D")
                    }
                }
            };

            var svc = new EvidenceRankingService();
            var result = await svc.RankAsync(new EvidenceRankingRequest
            {
                Query = "alternador",
                SessionClienteId = clientA,
                ScopedClienteId = clientA,
                ContextPackage = package
            });

            Assert.DoesNotContain(result.Ranked, r => r.EvidenceId == "EV-B");
            Assert.Contains(result.Ranked, r => r.EvidenceId == "EV-A");
            Assert.Contains(EvidenceRankingService.WarningCrossClientBlocked, result.Warnings);
            Assert.Contains(result.FilteredOutReasons, f => f.Contains("cross-client:EV-B"));
        }

        [Fact]
        public async Task Authorization_SessionMismatch_FailClosed()
        {
            var svc = new EvidenceRankingService();
            var result = await svc.RankAsync(new EvidenceRankingRequest
            {
                Query = "x",
                SessionClienteId = G("aaaaaaaa"),
                ScopedClienteId = G("bbbbbbbb"),
                Candidates = new[]
                {
                    new EvidenceItem { EvidenceId = "E1", Title = "x", Excerpt = "x", Kind = EvidenceKind.Knowledge }
                }
            });
            Assert.False(result.AuthorizationOk);
            Assert.Empty(result.Ranked);
            Assert.Contains(EvidenceRankingService.WarningAuthDenied, result.Warnings);
        }

        [Fact]
        public async Task Finance_Filtered_WhenNotAllowed()
        {
            var svc = new EvidenceRankingService();
            var result = await svc.RankAsync(new EvidenceRankingRequest
            {
                Query = "saldo",
                IncludeFinancial = true,
                CanIncludeFinance = false,
                Candidates = new[]
                {
                    new EvidenceItem { EvidenceId = "F1", Title = "saldo", Excerpt = "saldo", Classification = "FINANCIAL", Kind = EvidenceKind.Other },
                    new EvidenceItem { EvidenceId = "T1", Title = "tecnico", Excerpt = "saldo de carga", Classification = "TECHNICAL", Kind = EvidenceKind.Knowledge }
                }
            });
            Assert.DoesNotContain(result.Ranked, r => r.EvidenceId == "F1");
            Assert.Contains(result.Ranked, r => r.EvidenceId == "T1");
            Assert.Contains(EvidenceRankingService.WarningFinanceFiltered, result.Warnings);
        }

        [Fact]
        public async Task Pipeline_EmitsSourceTypeIdTitleExcerptRelevanceReason()
        {
            var svc = new EvidenceRankingService();
            var result = await svc.RankAsync(new EvidenceRankingRequest
            {
                Query = "bateria",
                Candidates = new[]
                {
                    new EvidenceItem
                    {
                        EvidenceId = "EV-BAT",
                        Kind = EvidenceKind.DiagnosticCase,
                        SourceCode = "CASE-9",
                        Title = "Bateria descarrega",
                        Excerpt = "consumo parasitico",
                        Classification = "TECHNICAL"
                    }
                }
            });
            Assert.Equal("AUTHORIZATION→FILTER→RETRIEVAL→RANKING", result.Pipeline);
            var item = Assert.Single(result.Ranked);
            Assert.Equal("DiagnosticCase", item.SourceType);
            Assert.Equal("CASE-9", item.SourceId);
            Assert.Equal("Bateria descarrega", item.Title);
            Assert.Equal("consumo parasitico", item.Excerpt);
            Assert.True(item.RelevanceScore > 0);
            Assert.False(string.IsNullOrWhiteSpace(item.RankingReason));
        }

        [Fact]
        public void SourceTypeWeights_AreDocumentedConstants()
        {
            Assert.Equal(1.5, EvidenceRankingService.ResolveSourceTypeWeight("Knowledge"));
            Assert.Equal(1.4, EvidenceRankingService.ResolveSourceTypeWeight("DiagnosticCase"));
            Assert.Equal(1.3, EvidenceRankingService.ResolveSourceTypeWeight("Procedure"));
            Assert.Equal(1.2, EvidenceRankingService.ResolveSourceTypeWeight("WorkOrderNote"));
            Assert.Equal(1.0, EvidenceRankingService.ResolveSourceTypeWeight("Other"));
        }
    }
}
