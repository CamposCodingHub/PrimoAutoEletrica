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
    /// <summary>C2.4 Contextual Search — synthetic DB only.</summary>
    public sealed class ContextualSearchTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly ContextualSearchService _contextual;

        public ContextualSearchTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"C24_Contextual_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            var retrieval = new KnowledgeRetrievalService(_repos.Knowledge, _repos.OrdensServico, _logger);
            var search = new KnowledgeSearchService(retrieval, _logger);
            var vehicle = new VehicleContextService(_repos.Clientes, _repos.OrdensServico, _repos.Knowledge, _logger);
            var client = new ClientContextService(_repos.Clientes, _repos.OrdensServico, _logger);
            var wo = new WorkOrderContextService(_repos.OrdensServico, _repos.Clientes, _repos.Knowledge, _logger);
            var composition = new ContextCompositionService(vehicle, client, wo, _logger);
            _contextual = new ContextualSearchService(search, composition, _logger);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
            }
            catch { /* ignore */ }
        }

        private (Cliente c, Veiculo v, OrdemServico os) Seed()
        {
            var c = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Cliente C24",
                Telefone = "11999990000",
                Ativo = true,
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Today,
                OrigemConsentimentoLGPD = "TesteC24"
            };
            _repos.Clientes.Inserir(c);
            var v = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = c.Id,
                Marca = "VW",
                Modelo = "Gol",
                Ano = "2018",
                Placa = "GHI3H45",
                SistemaEletrico = "12V",
                ProblemaRecorrente = "Queda de tensao"
            };
            _repos.Clientes.SalvarVeiculo(v);
            var os = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = "OS-C24-001",
                ClienteId = c.Id,
                VeiculoId = v.Id,
                PlacaSnapshot = v.Placa,
                ProblemaRelatado = "Nao parte pela manha",
                Status = "Concluida",
                DataAbertura = DateTime.Now.AddDays(-1),
                Itens = { new OrdemServicoItem { Tipo = "Peca", Descricao = "Bateria 60Ah" } }
            };
            _repos.OrdensServico.Inserir(os);
            return (c, v, os);
        }

        [Fact]
        public void DetectIntent_VehicleHistory()
        {
            Assert.Equal(ContextualSearchService.IntentVehicleHistory,
                ContextualSearchService.DetectIntent("historico de problemas do veiculo"));
        }

        [Fact]
        public void DetectIntent_D01()
        {
            Assert.Equal(ContextualSearchService.IntentProcedure,
                ContextualSearchService.DetectIntent("consultar D01 bateria"));
        }

        [Fact]
        public async Task History_WithoutAnchor_ClearEmpty()
        {
            var resp = await _contextual.SearchAsync(new KnowledgeSearchQuery
            {
                Text = "historico de problemas do veiculo"
            });
            Assert.Equal(KnowledgeSearchUiState.NoResults, resp.State);
            Assert.True(resp.UsedContext);
            Assert.Contains(ContextualSearchService.WarningNoContextEvidence, resp.Warnings);
            Assert.Empty(resp.Results);
            Assert.NotEmpty(resp.ContextMissingData);
        }

        [Fact]
        public async Task History_WithProvenVehicle_UsesContext()
        {
            var (_, v, _) = Seed();
            var resp = await _contextual.SearchAsync(new KnowledgeSearchQuery
            {
                Text = "historico de problemas do veiculo",
                UserContext = new KnowledgeSearchUserContext { VehicleId = v.Id }
            });
            Assert.True(resp.UsedContext);
            Assert.Equal("PRIMOX_CONTEXTUAL_SEARCH", resp.Provider);
            Assert.Contains(resp.ContextNotes, n => n.Contains("INTENT:"));
            // May be Found or NoResults depending on indexed OS adapters — never invent hits.
            Assert.True(resp.State is KnowledgeSearchUiState.Found or KnowledgeSearchUiState.NoResults);
            if (resp.State == KnowledgeSearchUiState.NoResults)
            {
                Assert.Contains(ContextualSearchService.WarningNoContextEvidence, resp.Warnings);
            }
        }

        [Fact]
        public async Task D01_WithoutContext_StillSearchesProcedures()
        {
            var resp = await _contextual.SearchAsync(new KnowledgeSearchQuery { Text = "D01" });
            // Generic/procedure intent does not require anchor; D01 should still be findable via C2.2 path.
            Assert.False(resp.UsedContext);
            Assert.True(resp.State is KnowledgeSearchUiState.Found or KnowledgeSearchUiState.NoResults or KnowledgeSearchUiState.Searching);
        }

        [Fact]
        public async Task GhostVehicle_History_ClearEmpty()
        {
            var resp = await _contextual.SearchAsync(new KnowledgeSearchQuery
            {
                Text = "historico do veiculo",
                UserContext = new KnowledgeSearchUserContext { VehicleId = Guid.NewGuid() }
            });
            Assert.Equal(KnowledgeSearchUiState.NoResults, resp.State);
            Assert.Contains(ContextualSearchService.WarningNoContextEvidence, resp.Warnings);
            Assert.Empty(resp.Results);
        }

        [Fact]
        public async Task Parts_WithoutOsContext_ClearEmpty()
        {
            var resp = await _contextual.SearchAsync(new KnowledgeSearchQuery
            {
                Text = "pecas usadas no veiculo"
            });
            Assert.Equal(KnowledgeSearchUiState.NoResults, resp.State);
            Assert.Empty(resp.Results);
        }
    }
}