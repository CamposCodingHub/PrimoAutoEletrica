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
    /// <summary>C2.5 Grounded Assist — synthetic DB; wires Context Engine into Assist.</summary>
    public sealed class GroundedAssistC25Tests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly AppSessionService _session;
        private readonly AuditLogService _audit;
        private readonly PermissionService _permissionService;
        private readonly AssistantService _assistant;
        private readonly Funcionario _tecnicoUser;

        public GroundedAssistC25Tests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"C25_Assist_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            _session = new AppSessionService();
            _audit = new AuditLogService(_database, _logger, _session);
            _tecnicoUser = new Funcionario
            {
                Id = 25,
                Nome = "Tecnico C25",
                Email = "c25@primoauto.com",
                PerfilAcesso = "Administrador"
            };
            _session.StartSession(_tecnicoUser);
            _permissionService = new PermissionService(_tecnicoUser, _logger);

            var retrieval = new KnowledgeRetrievalService(_repos.Knowledge, _repos.OrdensServico, _logger);
            var vehicle = new VehicleContextService(_repos.Clientes, _repos.OrdensServico, _repos.Knowledge, _logger);
            var client = new ClientContextService(_repos.Clientes, _repos.OrdensServico, _logger);
            var wo = new WorkOrderContextService(_repos.OrdensServico, _repos.Clientes, _repos.Knowledge, _logger);
            var composition = new ContextCompositionService(vehicle, client, wo, _logger);

            _assistant = new AssistantService(
                _repos.Knowledge,
                _repos.OrdensServico,
                _permissionService,
                _logger,
                _session,
                _audit,
                new GroundedLocalRuleAssistantProvider(),
                retrieval,
                composition);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true); } catch { }
        }

        [Fact]
        public async Task Assist_OutOfDomain_FailClosed()
        {
            var resp = await _assistant.ConsultarAsync("receita de bolo");
            Assert.Equal(AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE, resp.ConfidenceLevel);
            Assert.Contains(AssistFailClosedPolicy.WarningOutOfDomain, resp.Warnings);
            Assert.True(resp.Warnings.Contains(AssistFailClosedPolicy.WarningOutOfDomain) || resp.Provider == "PRIMOX_FAIL_CLOSED");
        }

        [Fact]
        public async Task Assist_WithOs_SurfacesContextMissingOrProven()
        {
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Cliente C25",
                Telefone = "11999990000",
                Ativo = true,
                ConsentimentoLGPD = true,
                DataConsentimentoLGPD = DateTime.Today,
                OrigemConsentimentoLGPD = "TesteC25"
            };
            _repos.Clientes.Inserir(cliente);
            var veiculo = new Veiculo
            {
                Id = Guid.NewGuid(),
                ClienteId = cliente.Id,
                Marca = "Fiat",
                Modelo = "Uno",
                Ano = "2015",
                Placa = "JKL4A56",
                SistemaEletrico = "12V"
            };
            _repos.Clientes.SalvarVeiculo(veiculo);
            var os = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = "OS-C25-001",
                ClienteId = cliente.Id,
                VeiculoId = veiculo.Id,
                ProblemaRelatado = "Queda de tensao sob carga",
                Status = "EmAndamento",
                DataAbertura = DateTime.Now
            };
            _repos.OrdensServico.Inserir(os);

            var resp = await _assistant.ConsultarAsync("queda de tensao", veiculoId: veiculo.Id, osId: os.Id);
            Assert.NotNull(resp.Provider);
            Assert.NotNull(resp.Timestamp);
            // Must not invent financial numbers
            Assert.DoesNotContain(resp.Evidence, e => e.Classification == "FINANCIAL");
            // Context wiring may add missing data lists (honest gaps) without crashing
            Assert.NotNull(resp.MissingInformation);
        }

                [Fact]
        public async Task Assist_NoEvidence_Insufficient()
        {
            var resp = await _assistant.ConsultarAsync("XYZ_NAO_EXISTE_PRIMOX_ASSIST_99999");
            Assert.False(string.IsNullOrWhiteSpace(resp.AnswerMarkdown));
            Assert.True(
                resp.ConfidenceLevel == AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE
                || (resp.Evidence.Count == 0 && resp.CitedSources.Count == 0),
                $"Expected insufficient/empty evidence, got {resp.ConfidenceLevel} evidence={resp.Evidence.Count} cited={resp.CitedSources.Count} answer={resp.AnswerMarkdown.Substring(0, Math.Min(120, resp.AnswerMarkdown.Length))}");
        }
    }
}