using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class AssistFoundationTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly AppSessionService _session;
        private readonly AuditLogService _audit;
        private readonly PermissionService _permissionService;
        private readonly AssistantService _assistantService;
        private readonly GroundedLocalRuleAssistantProvider _localProvider;
        private readonly Funcionario _tecnicoUser;

        public AssistFoundationTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"AssistTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            _session = new AppSessionService();
            _audit = new AuditLogService(_database, _logger, _session);

            _tecnicoUser = new Funcionario
            {
                Id = 10,
                Nome = "Eletricista Assist Test",
                Email = "tecnico@primoauto.com",
                PerfilAcesso = "Administrador"
            };
            _session.StartSession(_tecnicoUser);
            _permissionService = new PermissionService(_tecnicoUser, _logger);
            _localProvider = new GroundedLocalRuleAssistantProvider();

            _assistantService = new AssistantService(
                _repos.Knowledge,
                _repos.OrdensServico,
                _permissionService,
                _logger,
                _session,
                _audit,
                _localProvider);
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
                // ignore
            }
        }

        [Fact]
        public async Task GroundedLocalRuleAssistant_SemEvidencias_RetornaInsufficientEvidence()
        {
            var context = new AssistantQueryContext
            {
                Query = "Problema no teto solar que faz barulho de vento",
                RetrievedKnowledge = Array.Empty<TechnicalKnowledgeEntry>(),
                RetrievedCases = Array.Empty<DiagnosticCase>()
            };

            var response = await _localProvider.AskAsync(context);

            Assert.NotNull(response);
            Assert.Equal(AssistantConfidenceLevel.INSUFFICIENT_EVIDENCE, response.ConfidenceLevel);
            Assert.False(response.HasSufficientEvidence);
            Assert.Contains("Não encontrei evidência suficiente", response.AnswerMarkdown);
            Assert.NotEmpty(response.RecommendedActions);
            Assert.Empty(response.CitedSources);
        }

        [Fact]
        public async Task GroundedLocalRuleAssistant_ComEvidencias_RetornaRespostaEstruturadaComFontes()
        {
            var kb = new TechnicalKnowledgeEntry
            {
                KnowledgeId = Guid.NewGuid(),
                Code = "KB-ELET-001",
                Title = "Queda de Tensão no Circuito de Carga",
                System = "Carga",
                Voltage = "12V",
                Symptom = "Bateria descarrega com motor ligado",
                PossibleCauses = "Terminal oxidado; Queda de tensão no cabo positivo",
                DiagnosticProcedure = "Medir com multímetro B+ e B- sob carga",
                Solution = "Limpar terminais e substituir cabo se queda > 0.2V"
            };

            var caso = new DiagnosticCase
            {
                CaseId = Guid.NewGuid(),
                Code = "CASO-20260901-0001",
                Title = "Gol G5 - Alternador não carrega em alta rotação",
                VehicleModel = "VW Gol 1.6 2012",
                System = "Carga",
                Voltage = "12V",
                Symptom = "Luz de bateria acende ao acelerar",
                ConfirmedCause = "Regulador de voltagem com escovas gastas",
                Solution = "Substituição do regulador de voltagem e teste em bancada"
            };

            var context = new AssistantQueryContext
            {
                Query = "Alternador não carrega bateria",
                Vehicle = new AssistantVehicleContext
                {
                    Make = "Volkswagen",
                    Model = "Gol",
                    Year = 2012,
                    Voltage = "12V"
                },
                RetrievedKnowledge = new[] { kb },
                RetrievedCases = new[] { caso }
            };

            var response = await _localProvider.AskAsync(context);

            Assert.NotNull(response);
            Assert.True(response.HasSufficientEvidence);
            Assert.Equal(AssistantConfidenceLevel.HIGH, response.ConfidenceLevel);
            Assert.Contains("KB-ELET-001", response.AnswerMarkdown);
            Assert.Contains("CASO-20260901-0001", response.AnswerMarkdown);
            Assert.Equal(2, response.CitedSources.Count);
            Assert.Contains(response.CitedSources, c => c.SourceCode == "KB-ELET-001");
            Assert.Contains(response.CitedSources, c => c.SourceCode == "CASO-20260901-0001");
            Assert.NotEmpty(response.Hypotheses);
        }

        [Fact]
        public async Task GroundedLocalRuleAssistant_NuncaAfirmaTrocarPecaSemMedicao()
        {
            var kb = new TechnicalKnowledgeEntry
            {
                KnowledgeId = Guid.NewGuid(),
                Code = "KB-PART-001",
                Title = "Diagnóstico de Motor de Partida",
                System = "Partida",
                Voltage = "12V",
                Symptom = "Motor de partida pesado ao dar a partida",
                PossibleCauses = "Escovas gastas; Bucha gasta; Queda de tensão cabo negativo",
                DiagnosticProcedure = "Medir tensão nos bornes 30 e 50 durante o acionamento",
                Solution = "Reparar conforme teste elétrico"
            };

            var context = new AssistantQueryContext
            {
                Query = "Partida pesada",
                RetrievedKnowledge = new[] { kb },
                RetrievedCases = Array.Empty<DiagnosticCase>()
            };

            var response = await _localProvider.AskAsync(context);

            Assert.NotNull(response);
            Assert.Contains(response.RecommendedActions, a => a.Contains("Não substituir componentes elétricos antes de atestar"));
            Assert.NotEmpty(response.Hypotheses);
            foreach (var h in response.Hypotheses)
            {
                Assert.NotEmpty(h.RequiredVerificationTests);
            }
        }

        [Fact]
        public async Task AssistantService_ConsultaComOS_EnriqueceContextoEChamaProvider()
        {
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Transportadora Exemplo",
                Telefone = "11999998888",
                Ativo = true
            };
            _repos.Clientes.Inserir(cliente);

            var os = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = "OS-2026-9999",
                ClienteId = cliente.Id,
                ClienteNomeSnapshot = cliente.Nome,
                VeiculoDescricaoSnapshot = "Ford Cargo 2428 2011",
                PlacaSnapshot = "ABC1234",
                ProblemaRelatado = "Não carrega baterias 24V",
                Status = "EmAndamento",
                DataAbertura = DateTime.Now
            };
            _repos.OrdensServico.Inserir(os);

            var response = await _assistantService.ConsultarAsync("Alternador 24V com queda de carga", osId: os.Id);

            Assert.NotNull(response);
            Assert.NotNull(response.AnswerMarkdown);
            Assert.NotEmpty(response.RecommendedActions);
        }

        [Fact]
        public async Task AssistantService_SemPermissao_LancaUnauthorizedAccessException()
        {
            var semPermissaoUser = new Funcionario
            {
                Id = 99,
                Nome = "Usuário Sem Permissão",
                Email = "sempermissao@primoauto.com",
                PerfilAcesso = "Atendente"
            };
            var sessionSemPerm = new AppSessionService();
            sessionSemPerm.StartSession(semPermissaoUser);
            // PermissionService configurado para negar ASSIST_UTILIZAR
            var permissionServiceNegado = new PermissionService(semPermissaoUser, code => false, _logger);

            var serviceRestrito = new AssistantService(
                _repos.Knowledge,
                _repos.OrdensServico,
                permissionServiceNegado,
                _logger,
                sessionSemPerm,
                _audit,
                _localProvider);

            var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                serviceRestrito.ConsultarAsync("Consulta técnica"));

            Assert.Contains("ASSIST_UTILIZAR", ex.Message);
        }
    }
}
