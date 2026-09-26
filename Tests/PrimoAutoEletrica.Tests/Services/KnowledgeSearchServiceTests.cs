using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>
    /// C2.2 — Deterministic PRIMOX search. Synthetic isolated DBs only.
    /// </summary>
    public sealed class KnowledgeSearchServiceTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly KnowledgeRetrievalService _retrieval;
        private readonly KnowledgeSearchService _search;

        public KnowledgeSearchServiceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"C22_SearchSynthetic_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            _retrieval = new KnowledgeRetrievalService(_repos.Knowledge, _repos.OrdensServico, _logger);
            _search = new KnowledgeSearchService(_retrieval, _logger);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
            }
            catch { /* cleanup race */ }
        }

        [Fact]
        public void Normalize_TrimCollapseAccent_PreservesTechnicalTokens()
        {
            var n = KnowledgeQueryNormalizer.Normalize("  Queda   de   TENSÃO  ");
            Assert.Equal("queda de tensao", n);

            var tokens = KnowledgeQueryNormalizer.Tokenize("D01 24V 12V CAN ABS ECU queda de tensão");
            Assert.Contains("d01", tokens);
            Assert.Contains("24v", tokens);
            Assert.Contains("12v", tokens);
            Assert.Contains("can", tokens);
            Assert.Contains("abs", tokens);
            Assert.Contains("ecu", tokens);
            Assert.Contains("queda", tokens);
            Assert.Contains("tensao", tokens);
            Assert.DoesNotContain("de", tokens);
        }

        [Fact]
        public async Task EmptyQuery_DoesNotDumpAll_ReturnsMessage()
        {
            await _retrieval.RebuildIndexAsync();
            var resp = await _search.SearchAsync("   ");
            Assert.Equal(KnowledgeSearchUiState.NoQuery, resp.State);
            Assert.Equal(KnowledgeSearchService.EmptyQueryMessage, resp.Message);
            Assert.Empty(resp.Results);
            Assert.Empty(resp.Groups);
        }

        [Fact]
        public async Task NoResults_UnknownQuery_EmptyState()
        {
            await _retrieval.RebuildIndexAsync();
            var resp = await _search.SearchAsync("XYZ_NAO_EXISTE_PRIMOX_999");
            Assert.Equal(KnowledgeSearchUiState.NoResults, resp.State);
            Assert.Empty(resp.Results);
            Assert.Empty(resp.Groups);
            Assert.Contains("Nenhum resultado", resp.Message);
        }

        [Theory]
        [InlineData("D01")]
        [InlineData("D17")]
        [InlineData("queda de tensão")]
        [InlineData("alternador")]
        [InlineData("24V")]
        public async Task ExactQueries_ReturnHits(string q)
        {
            await _retrieval.RebuildIndexAsync();
            var resp = await _search.SearchAsync(q);
            Assert.Equal(KnowledgeSearchUiState.Found, resp.State);
            Assert.NotEmpty(resp.Results);
            Assert.All(resp.Results, r =>
            {
                Assert.False(string.IsNullOrWhiteSpace(r.SourceType));
                Assert.False(string.IsNullOrWhiteSpace(r.SourceId));
                Assert.NotNull(r.Evidence);
            });
        }

        [Fact]
        public async Task D01_to_D17_IndividualSearches_17of17()
        {
            await _retrieval.RebuildIndexAsync();
            var rows = new List<(string Query, int Count, string Source, string Type)>();
            for (var n = 1; n <= 17; n++)
            {
                var code = $"D{n:00}";
                var resp = await _search.SearchAsync(code);
                Assert.Equal(KnowledgeSearchUiState.Found, resp.State);
                Assert.Contains(resp.Results, r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase));
                var hit = resp.Results.First(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase));
                rows.Add((code, resp.TotalCount, hit.SourceType, hit.Type));
            }

            Assert.Equal(17, rows.Count);
            Assert.Equal(17, rows.Select(r => r.Query).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public async Task Grouping_OnlyNonEmptyCategories()
        {
            await _retrieval.RebuildIndexAsync();
            var resp = await _search.SearchAsync("D01");
            Assert.NotEmpty(resp.Groups);
            Assert.All(resp.Groups, g => Assert.NotEmpty(g.Items));
            Assert.Contains(resp.Groups, g => g.Key == KnowledgeSearchGroups.Procedimentos);
        }

        [Fact]
        public async Task Ranking_ExactCodePreferredOverLooseText()
        {
            await _retrieval.RebuildIndexAsync();
            var resp = await _search.SearchAsync("D01");
            Assert.NotEmpty(resp.Results);
            Assert.Equal("D01", resp.Results[0].Code);
        }

        [Fact]
        public async Task Evidence_OnlyRealFound_NoSilentFill()
        {
            await _retrieval.RebuildIndexAsync();
            var resp = await _search.SearchAsync("queda de tensão");
            Assert.NotEmpty(resp.Results);
            foreach (var r in resp.Results)
            {
                Assert.All(r.Evidence, e =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(e.SourceCode) && string.IsNullOrWhiteSpace(e.Title));
                    Assert.NotEqual("fault probability", e.RelevanceLabel);
                });
            }
        }

        [Fact]
        public async Task RBAC_FiltersFinancialPurchase_WithoutPermission()
        {
            await _retrieval.RebuildIndexAsync();
            var purchase = new KnowledgeItem
            {
                ItemId = "fin-1",
                Type = KnowledgeType.PURCHASE,
                Code = "FIN-TEST-001",
                Title = "Pedido compra alternador financeiro",
                Symptom = "compra alternador",
                BodyText = "ordem de compra alternador valor",
                SourceEntity = "PurchaseRequest",
                SourceEntityId = "PR-1",
                Classification = "FINANCIAL",
                Tags = new[] { "alternador", "compra" }
            };
            _retrieval.Index.Upsert(purchase);

            var denied = await _search.SearchAsync(new KnowledgeSearchQuery
            {
                Text = "alternador",
                HasFinancePermission = false,
                MaxResults = 50
            });
            Assert.DoesNotContain(denied.Results, r => r.Code == "FIN-TEST-001");
            Assert.Contains(KnowledgeSearchService.RbacFilteredWarning, denied.Warnings);

            var allowed = await _search.SearchAsync(new KnowledgeSearchQuery
            {
                Text = "alternador",
                HasFinancePermission = true,
                MaxResults = 50
            });
            Assert.Contains(allowed.Results, r => r.Code == "FIN-TEST-001");
        }

        [Fact]
        public async Task RBAC_DirectSourceIdAccess_DeniedWithoutPermission()
        {
            await _retrieval.RebuildIndexAsync();
            var purchase = new KnowledgeItem
            {
                ItemId = "fin-2",
                Type = KnowledgeType.PURCHASE,
                Code = "FIN-TEST-002",
                Title = "Compra secreta",
                BodyText = "financeiro",
                SourceEntity = "PurchaseRequest",
                SourceEntityId = "PR-SECRET",
                Classification = "FINANCIAL"
            };
            _retrieval.Index.Upsert(purchase);

            Assert.Null(_search.GetBySourceId("PurchaseRequest", "PR-SECRET", hasFinancePermission: false));
            Assert.NotNull(_search.GetBySourceId("PurchaseRequest", "PR-SECRET", hasFinancePermission: true));
        }

        [Fact]
        public async Task Exception_FromFaultyRetrieval_ControlledError_NoStack()
        {
            var faulty = new FaultyRetrieval();
            var svc = new KnowledgeSearchService(faulty, _logger);
            var resp = await svc.SearchAsync("alternador");
            Assert.Equal(KnowledgeSearchUiState.Error, resp.State);
            Assert.Equal(KnowledgeSearchService.ErrorMessage, resp.Message);
            Assert.DoesNotContain("at ", resp.Message);
            Assert.DoesNotContain("Stack", resp.ErrorDetail ?? string.Empty);
        }

        [Fact]
        public async Task Concurrency_TwoParallelSearches_NoCrossContamination()
        {
            await _retrieval.RebuildIndexAsync();
            var t1 = _search.SearchAsync("D01");
            var t2 = _search.SearchAsync("D17");
            var results = await Task.WhenAll(t1, t2);
            Assert.Equal("D01", results[0].Query);
            Assert.Equal("D17", results[1].Query);
            Assert.Equal("D01", results[0].Results[0].Code);
            Assert.Equal("D17", results[1].Results[0].Code);
            Assert.DoesNotContain(results[0].Results, r => r.Code.Equals("D17", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(results[1].Results, r => r.Code.Equals("D01", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void LargeSynthetic_DatasetTimings_Recorded()
        {
            var index = new DeterministicKnowledgeIndex();
            for (var i = 0; i < 1000; i++)
            {
                index.Upsert(new KnowledgeItem
                {
                    ItemId = $"syn-{i}",
                    Type = i % 5 == 0 ? KnowledgeType.PROCEDURE : KnowledgeType.TECHNICAL_CASE,
                    Code = i < 17 ? $"D{i + 1:00}" : $"SYN-{i:0000}",
                    Title = $"Item sintetico {i} alternador queda tensao",
                    Symptom = i % 2 == 0 ? "queda de tensão" : "partida",
                    BodyText = $"conteudo sintetico {i} 24V 12V CAN ABS ECU",
                    Tags = new[] { "sintetico", i % 3 == 0 ? "alternador" : "bateria" },
                    SourceEntity = "Synthetic",
                    SourceEntityId = i.ToString()
                });
            }

            var sw100 = Stopwatch.StartNew();
            var q100 = index.Search(new KnowledgeSearchQuery { Text = "alternador", MaxResults = 20 });
            sw100.Stop();

            for (var i = 1000; i < 10000; i++)
            {
                index.Upsert(new KnowledgeItem
                {
                    ItemId = $"syn-{i}",
                    Type = KnowledgeType.OTHER,
                    Code = $"SYN-{i:0000}",
                    Title = $"Bulk {i}",
                    BodyText = $"bulk content {i}",
                    SourceEntity = "Synthetic",
                    SourceEntityId = i.ToString()
                });
            }

            var sw10k = Stopwatch.StartNew();
            var q10k = index.Search(new KnowledgeSearchQuery { Text = "alternador", MaxResults = 20 });
            sw10k.Stop();

            Assert.True(index.Count >= 10000);
            Assert.NotEmpty(q100.Hits);
            Assert.NotEmpty(q10k.Hits);
            Assert.True(sw100.ElapsedMilliseconds >= 0);
            Assert.True(sw10k.ElapsedMilliseconds >= 0);
            _logger.LogInfo($"C2.2 large synthetic timings: n~1000 alternador={sw100.ElapsedMilliseconds}ms hits={q100.Hits.Count}; n=10000 alternador={sw10k.ElapsedMilliseconds}ms hits={q10k.Hits.Count}");
        }

        [Fact]
        public async Task Performance_SampleQueries_RecordRealMs()
        {
            await _retrieval.RebuildIndexAsync();
            string[] queries = { "D01", "D17", "queda de tensão", "alternador", "24V" };
            foreach (var q in queries)
            {
                var sw = Stopwatch.StartNew();
                var resp = await _search.SearchAsync(q);
                sw.Stop();
                Assert.True(resp.DurationMs >= 0);
                _logger.LogInfo($"C2.2 perf query='{q}' durationMs={resp.DurationMs} wall={sw.ElapsedMilliseconds} state={resp.State} count={resp.TotalCount}");
            }
        }

        private sealed class FaultyRetrieval : IKnowledgeRetrievalService
        {
            public DeterministicKnowledgeIndex Index { get; } = new();
            public Task<int> RebuildIndexAsync(System.Threading.CancellationToken ct = default) => Task.FromResult(0);
            public Task<KnowledgeSearchResult> SearchAsync(string query, int maxResults = 20, KnowledgeType? typeFilter = null, System.Threading.CancellationToken ct = default)
                => throw new InvalidOperationException("synthetic adapter failure");
            public Task<KnowledgeSearchResult> SearchAsync(KnowledgeSearchQuery query, System.Threading.CancellationToken ct = default)
                => throw new InvalidOperationException("synthetic adapter failure");
            public IReadOnlyList<KnowledgeItem> GetIndexedProceduresD01ToD17() => Array.Empty<KnowledgeItem>();
        }
    }
}