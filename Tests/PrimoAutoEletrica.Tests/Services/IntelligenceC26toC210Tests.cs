using System;
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
    public sealed class IntelligenceC26toC210Tests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly ContextCompositionService _composition;
        private readonly Intelligence360Enricher _enricher;
        private readonly ContextualSearchService _contextual;
        private readonly DiagnosticIntelligenceService _diagnostic;
        private readonly KnowledgePromotionService _promotion;
        private readonly IntelligenceAuditService _audit;

        public IntelligenceC26toC210Tests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"C26_210_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            var vehicle = new VehicleContextService(_repos.Clientes, _repos.OrdensServico, _repos.Knowledge, _logger);
            var client = new ClientContextService(_repos.Clientes, _repos.OrdensServico, _logger);
            var wo = new WorkOrderContextService(_repos.OrdensServico, _repos.Clientes, _repos.Knowledge, _logger);
            _composition = new ContextCompositionService(vehicle, client, wo, _logger);
            _enricher = new Intelligence360Enricher(_composition);
            var retrieval = new KnowledgeRetrievalService(_repos.Knowledge, _repos.OrdensServico, _logger);
            var search = new KnowledgeSearchService(retrieval, _logger);
            _contextual = new ContextualSearchService(search, _composition, _logger);
            _diagnostic = new DiagnosticIntelligenceService(_contextual, _composition, retrieval);
            _promotion = new KnowledgePromotionService(_repos.OrdensServico, _repos.Knowledge, wo);
            _audit = new IntelligenceAuditService();
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true); } catch { }
        }

        private (Cliente c, Veiculo v, OrdemServico os) SeedCompleteOs()
        {
            var c = new Cliente
            {
                Id = Guid.NewGuid(), Nome = "Cliente C26", Telefone = "11999990000", Ativo = true,
                ConsentimentoLGPD = true, DataConsentimentoLGPD = DateTime.Today, OrigemConsentimentoLGPD = "t"
            };
            _repos.Clientes.Inserir(c);
            var v = new Veiculo
            {
                Id = Guid.NewGuid(), ClienteId = c.Id, Marca = "GM", Modelo = "Onix", Ano = "2019",
                Placa = "MNO5B67", SistemaEletrico = "12V", ProblemaRecorrente = "Queda de tensao"
            };
            _repos.Clientes.SalvarVeiculo(v);
            var os = new OrdemServico
            {
                Id = Guid.NewGuid(), Numero = "OS-C26-001", ClienteId = c.Id, VeiculoId = v.Id,
                ProblemaRelatado = "Nao carrega bateria", Diagnostico = "Alternador com falha",
                DiagnosticoFinal = "Alternador com falha de diodo", ObservacoesInternas = "Substituir alternador",
                Status = "Concluida", DataAbertura = DateTime.Now.AddDays(-1),
                Itens = { new OrdemServicoItem { Tipo = "Peca", Descricao = "Alternador reman" } }
            };
            _repos.OrdensServico.Inserir(os);
            return (c, v, os);
        }

        [Fact]
        public async Task C26_Vehicle360_Overlay_ProvenOnly()
        {
            var (_, v, _) = SeedCompleteOs();
            var overlay = await _enricher.ForVehicleAsync(v.Id);
            Assert.True(overlay.Found);
            Assert.Contains(overlay.Alerts, a => a == "RECURRING_PROBLEM_ON_RECORD");
            Assert.All(overlay.ProvenFacts, f => Assert.Equal(ContextProvenanceStatus.PROVEN, f.Status));
        }

        [Fact]
        public async Task C27_Diagnostic_NotGuaranteed()
        {
            var (_, v, os) = SeedCompleteOs();
            var result = await _diagnostic.AnalyzeAsync(new DiagnosticIntelligenceRequest
            {
                SymptomQuery = "queda de tensao",
                VehicleId = v.Id,
                WorkOrderId = os.Id
            });
            Assert.False(result.IsGuaranteedDiagnosis);
            Assert.Contains("NOT_A_GUARANTEED_DIAGNOSIS", result.Warnings);
            Assert.Contains("diagnóstico garantido", result.Disclaimer, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task C28_Promotion_RequiresHumanApproval_NoAutoPublish()
        {
            var (_, _, os) = SeedCompleteOs();
            var cand = await _promotion.ProposeFromWorkOrderAsync(os.Id, 1, "Autor");
            Assert.NotNull(cand);
            Assert.False(cand!.AutoPublished);
            Assert.Equal(KnowledgePromotionStatus.Pending, cand.Status);
            Assert.NotEmpty(cand.EvidenceOrigins);

            var ok = await _promotion.ApproveAsync(cand.CandidateId, 2, "Aprovador");
            Assert.True(ok);
            Assert.Equal(KnowledgePromotionStatus.Approved, cand.Status);
            Assert.False(cand.AutoPublished);
            Assert.Contains("no auto-publish", cand.DecisionReason ?? "", StringComparison.OrdinalIgnoreCase);

            var arts = await _repos.Knowledge.ObterArtigosAsync();
            Assert.DoesNotContain(arts, a => a.Status == KnowledgeStatus.PUBLISHED && (a.Tags?.Contains("c2.8") ?? false));
        }

        [Fact]
        public async Task C28_InsufficientOs_NoCandidate()
        {
            var c = new Cliente
            {
                Id = Guid.NewGuid(), Nome = "Incompleto", Telefone = "11999990000", Ativo = true,
                ConsentimentoLGPD = true, DataConsentimentoLGPD = DateTime.Today, OrigemConsentimentoLGPD = "t"
            };
            _repos.Clientes.Inserir(c);
            var os = new OrdemServico
            {
                Id = Guid.NewGuid(), Numero = "OS-INSUF", ClienteId = c.Id,
                ProblemaRelatado = "", Diagnostico = "", Status = "Aberta", DataAbertura = DateTime.Now
            };
            _repos.OrdensServico.Inserir(os);
            var cand = await _promotion.ProposeFromWorkOrderAsync(os.Id, 1, "Autor");
            Assert.Null(cand);
        }

        [Fact]
        public void C29_Audit_RedactsSecrets()
        {
            _audit.Record(new IntelligenceAuditEntry
            {
                Question = "queda de tensao",
                UserName = "tech",
                AnswerSummary = "ok",
                Provider = "PRIMOX_LOCAL_GROUNDED",
                Result = "OK",
                EvidenceSummary = "Authorization: Bearer SECRETTOKEN api_key=xyz"
            });
            var recent = _audit.ListRecent(1);
            Assert.Single(recent);
            Assert.Equal("[REDACTED]", recent[0].EvidenceSummary);
            Assert.DoesNotContain("SECRETTOKEN", recent[0].EvidenceSummary);
        }

        // C2.10 Adversarial fail-closed
        [Fact]
        public async Task C210_NoData_FailClosed()
        {
            var overlay = await _enricher.ForVehicleAsync(Guid.NewGuid());
            Assert.False(overlay.Found);
        }

        [Fact]
        public async Task C210_WrongVehicle_NoInventedHistory()
        {
            var resp = await _contextual.SearchAsync(new KnowledgeSearchQuery
            {
                Text = "historico de problemas do veiculo",
                UserContext = new KnowledgeSearchUserContext { VehicleId = Guid.NewGuid() }
            });
            Assert.Equal(KnowledgeSearchUiState.NoResults, resp.State);
            Assert.Empty(resp.Results);
        }

        [Fact]
        public async Task C210_MissingId_FailClosed()
        {
            var overlay = await _enricher.ForWorkOrderAsync(Guid.Empty);
            Assert.False(overlay.Found);
            Assert.Contains("ANCHOR_ID_EMPTY", overlay.Alerts);
        }

        [Fact]
        public async Task C210_CrossClient_CompositionDenied()
        {
            var (c, _, os) = SeedCompleteOs();
            var composed = await _composition.ComposeAsync(new ContextCompositionRequest
            {
                WorkOrderId = os.Id,
                SessionClienteId = Guid.NewGuid()
            });
            Assert.False(composed.HasAnyProvenAnchor);
            Assert.Contains(ContextCompositionService.WarningCrossClientDenied, composed.Warnings);
        }

        [Fact]
        public async Task C210_InduceFinancial_NoFinanceInOverlay()
        {
            var (c, _, _) = SeedCompleteOs();
            var overlay = await _enricher.ForClientAsync(c.Id);
            Assert.DoesNotContain(overlay.ProvenFacts, f => f.Classification == "FINANCIAL");
            Assert.DoesNotContain(overlay.ProvenFacts, f => (f.Value ?? "").Contains("TotalGasto", StringComparison.OrdinalIgnoreCase));
        }
    }
}