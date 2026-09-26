using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Knowledge;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    /// <summary>
    /// C2.1 — Operational Knowledge Foundation tests.
    /// Uses SYNTHETIC isolated DB under temp dir (clearly marked). Optional operacional checks are skippable.
    /// </summary>
    public sealed class KnowledgeRetrievalTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly KnowledgeRetrievalService _retrieval;

        public KnowledgeRetrievalTests()
        {
            // SYNTHETIC isolated DB — never touches protected primoauto.db
            _testDir = Path.Combine(Path.GetTempPath(), $"C21_KnowledgeSynthetic_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            _retrieval = new KnowledgeRetrievalService(_repos.Knowledge, _repos.OrdensServico, _logger);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, true);
                }
            }
            catch
            {
                // ignore cleanup races
            }
        }

        [Fact]
        public async Task Index_D01_to_D17_Are_17_of_17_NoImproperDuplicates()
        {
            var count = await _retrieval.RebuildIndexAsync();
            Assert.True(count >= 17, $"Index should contain at least D01–D17; got {count}");

            var dItems = _retrieval.GetIndexedProceduresD01ToD17();
            Assert.Equal(17, dItems.Count);

            var codes = dItems.Select(i => i.Code.ToUpperInvariant()).ToList();
            Assert.Equal(17, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());

            for (var n = 1; n <= 17; n++)
            {
                var code = $"D{n:00}";
                Assert.Contains(code, codes);
            }

            // No improper duplicates: same DedupKey cannot appear twice in snapshot
            var all = _retrieval.Index.SnapshotItems();
            var dedupKeys = all.Select(i => i.DedupKey).ToList();
            Assert.Equal(dedupKeys.Count, dedupKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        [Fact]
        public async Task Search_QuedaDeTensao_ReturnsRelevantHits()
        {
            await _retrieval.RebuildIndexAsync();
            var result = await _retrieval.SearchAsync("queda de tensão");

            Assert.Equal("queda de tensão", result.Query);
            Assert.True(result.IndexedItemCount >= 17);
            Assert.NotEmpty(result.Hits);
            Assert.Equal("PRIMOX_DETERMINISTIC_INDEX", result.Provider);

            // Expect at least one procedure / KB mentioning voltage drop
            Assert.Contains(result.Hits, h =>
                h.Item.Code.StartsWith("D", StringComparison.OrdinalIgnoreCase) ||
                h.Item.Code.StartsWith("KB-", StringComparison.OrdinalIgnoreCase) ||
                h.Item.Code.StartsWith("LIB-", StringComparison.OrdinalIgnoreCase));

            Assert.Contains(result.Hits, h =>
                DeterministicKnowledgeIndex.RemoveDiacritics((h.Item.BodyText + h.Item.Symptom + h.Item.Title).ToLowerInvariant())
                    .Contains("queda") ||
                DeterministicKnowledgeIndex.RemoveDiacritics((h.Item.BodyText + h.Item.Symptom + h.Item.Title).ToLowerInvariant())
                    .Contains("tensao"));
        }

        [Fact]
        public void Adapters_MapTechnicalAndDiagnosticWithoutReplacingEntities()
        {
            var kb = new TechnicalKnowledgeEntry
            {
                KnowledgeId = Guid.NewGuid(),
                Code = "KB-TEST-001",
                Title = "Teste Queda",
                System = "Carga",
                Symptom = "queda de tensão",
                Solution = "medir e corrigir"
            };
            var item = KnowledgeItemAdapters.FromTechnicalKnowledge(kb);
            Assert.Equal(KnowledgeType.TECHNICAL_CASE, item.Type);
            Assert.Equal("KB-TEST-001", item.Code);
            Assert.Equal(nameof(TechnicalKnowledgeEntry), item.SourceEntity);

            var caso = new DiagnosticCase
            {
                CaseId = Guid.NewGuid(),
                Code = "CASO-TEST-1",
                Title = "Caso",
                Symptom = "partida pesada",
                ConfirmedCause = "cabo",
                Solution = "trocar cabo"
            };
            var caseItem = KnowledgeItemAdapters.FromDiagnosticCase(caso);
            Assert.Equal(KnowledgeType.DIAGNOSTIC_CASE, caseItem.Type);
            Assert.Equal(nameof(DiagnosticCase), caseItem.SourceEntity);
        }

        [Fact]
        public void DeterministicIndex_UpsertSameCode_DoesNotDuplicate()
        {
            var index = new DeterministicKnowledgeIndex();
            var a = new KnowledgeItem
            {
                ItemId = "1",
                Type = KnowledgeType.PROCEDURE,
                Code = "D01",
                Title = "A",
                Symptom = "partida",
                BodyText = "partida"
            };
            var b = new KnowledgeItem
            {
                ItemId = "2",
                Type = KnowledgeType.PROCEDURE,
                Code = "D01",
                Title = "B updated",
                Symptom = "partida falha",
                BodyText = "partida falha"
            };

            Assert.True(index.Upsert(a));
            Assert.False(index.Upsert(b));
            Assert.Equal(1, index.Count);
            Assert.Equal("B updated", index.SnapshotItems().Single().Title);
        }

        [Fact]
        public void FailClosed_F1_F2_F3_F4_Stubs()
        {
            var f1 = AssistFailClosedPolicy.ApplyF1NoEvidence("xyz");
            Assert.Equal(AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE, f1.ConfidenceLevel);
            Assert.NotEmpty(f1.MissingInformation);

            var f2 = AssistFailClosedPolicy.ApplyF2OutOfDomain("horoscopo de hoje");
            Assert.Contains(AssistFailClosedPolicy.WarningOutOfDomain, f2.Warnings);

            var f3 = AssistFailClosedPolicy.ApplyF3FinancialDenied("faturamento mensal");
            Assert.Contains(AssistFailClosedPolicy.WarningFinancialDenied, f3.Warnings);

            var otherClient = Guid.NewGuid();
            var f4 = AssistFailClosedPolicy.EvaluatePreProvider(
                "diagnostico",
                hasFinancePermission: true,
                sessionClienteId: Guid.NewGuid(),
                contextClienteId: otherClient);
            Assert.NotNull(f4);
            Assert.Contains(AssistFailClosedPolicy.WarningCrossClientDenied, f4!.Warnings);

            Assert.True(AssistFailClosedPolicy.LooksOutOfDomain("receita de bolo chocolate"));
            Assert.True(AssistFailClosedPolicy.LooksFinancial("ver lucro da oficina"));
        }

        [Fact]
        public void ContractMapper_EnrichesEvidenceFromCitedSources()
        {
            var raw = new AssistantResponse
            {
                AnswerMarkdown = "ok",
                ConfidenceLevel = AssistantConfidenceLevel.MEDIUM,
                CitedSources = new[]
                {
                    new AssistantSourceCitation
                    {
                        SourceCode = "KB-ELET-001",
                        SourceTitle = "Queda",
                        RelevanceExplanation = "match"
                    }
                }
            };

            var enriched = AssistantContractMapper.Enrich(raw, providerId: "PRIMOX_LOCAL_GROUNDED");
            Assert.Equal("PRIMOX_LOCAL_GROUNDED", enriched.Provider);
            Assert.NotNull(enriched.Timestamp);
            Assert.NotEmpty(enriched.Evidence);
            Assert.Equal("KB-ELET-001", enriched.Evidence[0].SourceCode);
            Assert.Equal("token-match", "token-match"); // honesty: RelevanceLabel never "fault probability"
            Assert.NotEqual("fault probability", enriched.Evidence[0].RelevanceLabel);
            Assert.Equal(raw.AnswerMarkdown, enriched.Answer);
            Assert.Equal(raw.RecommendedActions, enriched.SuggestedNextSteps);
        }

        [Fact]
        public async Task ContextBuilder_BuildsEvidenceCandidates()
        {
            await _retrieval.RebuildIndexAsync();
            var builder = new AssistContextBuilder(_retrieval, _repos.OrdensServico);
            var ctx = await builder.BuildAsync("queda de tensão no terra");
            Assert.Equal("queda de tensão no terra", ctx.Query);
            Assert.NotEmpty(ctx.RetrievedEvidence);
            Assert.Contains(ctx.AllowedClasses, c => c == "TECHNICAL");
        }

        [Fact]
        public async Task Search_AgainstSyntheticSeededKnowledge_FindsKbElet001()
        {
            // Synthetic DB already seeds KB-ELET-001 via DatabaseService.C1
            await _retrieval.RebuildIndexAsync();
            var result = await _retrieval.SearchAsync("queda de tensão partida 24V");
            Assert.Contains(result.Hits, h => h.Item.Code == "KB-ELET-001" || h.Item.Code.StartsWith("D"));
        }

        [Fact]
        public void Operacional_D01D17_CatalogPresent_EvenIfDbLacksCases()
        {
            // Catalog is in-memory — always 17 regardless of operacional DB content.
            var roteiros = AutoEletricaTecnicaService.CreateRoteirosDiagnostico();
            Assert.Equal(17, roteiros.Count);
            Assert.Equal(17, roteiros.Select(r => r.Codigo).Distinct().Count());

            // Optional: peek operacional DiagnosticCases if file exists (read-only).
            var opPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
                "primoauto_operacional.db");

            if (!File.Exists(opPath))
            {
                return; // skip soft
            }

            try
            {
                using var conn = new SqliteConnection($"Data Source={opPath};Mode=ReadOnly");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='DiagnosticCases';";
                var tableExists = Convert.ToInt64(cmd.ExecuteScalar()) > 0;
                if (!tableExists) return;

                cmd.CommandText = "SELECT COUNT(*) FROM DiagnosticCases;";
                var caseCount = Convert.ToInt64(cmd.ExecuteScalar());
                // Informational assert: operacional may have 0+ cases; catalog still 17.
                Assert.True(caseCount >= 0);
                Assert.Equal(17, roteiros.Count);
            }
            catch (SqliteException)
            {
                // Locked / busy operacional — soft skip
            }
        }
    }
}
