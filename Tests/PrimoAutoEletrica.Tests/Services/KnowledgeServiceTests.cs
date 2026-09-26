using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public sealed class KnowledgeServiceTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly AppSessionService _session;
        private readonly AuditLogService _audit;
        private readonly PermissionService _permissionService;
        private readonly KnowledgeService _knowledgeService;
        private readonly Funcionario _adminUser;

        public KnowledgeServiceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"KnowledgeTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            _session = new AppSessionService();
            _audit = new AuditLogService(_database, _logger, _session);

            _adminUser = new Funcionario
            {
                Id = 1,
                Nome = "Engenheiro Diagnóstico",
                Email = "diag@primoauto.com",
                PerfilAcesso = "Administrador"
            };
            _session.StartSession(_adminUser);
            _permissionService = new PermissionService(_adminUser, _logger);

            _knowledgeService = new KnowledgeService(
                _repos.Knowledge,
                _repos.OrdensServico,
                _permissionService,
                _logger,
                _session,
                _audit);
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
        public async Task ListarArtigos_RetornaArtigosSeededD01D02()
        {
            var artigos = await _knowledgeService.ListarArtigosAsync();
            Assert.NotNull(artigos);
            Assert.True(artigos.Count >= 2, "Deveria conter artigos técnicos iniciais do seed.");
            Assert.Contains(artigos, a => a.Code == "KB-ELET-001");
            Assert.Contains(artigos, a => a.Code == "KB-CAN-002");
        }

        [Fact]
        public async Task SalvarArtigo_CadastraEAtualizaArtigo()
        {
            var artigo = new TechnicalKnowledgeEntry
            {
                KnowledgeId = Guid.NewGuid(),
                Code = "KB-TEST-99",
                Title = "Teste de Fuga de Corrente em Módulo Eletrônico",
                System = "Alimentação e Bateria",
                VehicleCategory = "Linha Leve",
                Voltage = "12V",
                Symptom = "Bateria descarrega totalmente em 48 horas parada.",
                PossibleCauses = "Módulo de conforto não entra em modo sleep; alarme com consumo parasita.",
                DiagnosticProcedure = "Instalar alicate amperimétrico em escala DC mA no polo negativo com chave desligada.",
                RecommendedMeasurements = "Consumo em repouso pós-sleep <= 50mA.",
                Solution = "Reparar chicote do microinterruptor da porta do motorista.",
                CreatedByUserId = 1,
                CreatedByUserName = "Engenheiro Teste"
            };

            var salvo = await _knowledgeService.SalvarArtigoAsync(artigo);
            Assert.True(salvo);

            var recuperado = await _knowledgeService.ObterArtigoPorCodigoAsync("KB-TEST-99");
            Assert.NotNull(recuperado);
            Assert.Equal("Teste de Fuga de Corrente em Módulo Eletrônico", recuperado.Title);

            // Atualização
            recuperado.Title = "Teste Avançado de Fuga de Corrente em Módulo Eletrônico";
            var atualizado = await _knowledgeService.SalvarArtigoAsync(recuperado);
            Assert.True(atualizado);

            var posAtualizado = await _knowledgeService.ObterArtigoPorIdAsync(artigo.KnowledgeId);
            Assert.NotNull(posAtualizado);
            Assert.Equal("Teste Avançado de Fuga de Corrente em Módulo Eletrônico", posAtualizado.Title);
        }

        [Fact]
        public async Task CriarCasoAPartirDeOS_ExtraiDadosDaOS_SalvaCasoComVoltagemAdequada()
        {
            // Criar uma OS de linha pesada (Mercedes Actros)
            var os = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Numero = "OS-2026-9999",
                VeiculoId = Guid.NewGuid(),
                VeiculoDescricaoSnapshot = "Mercedes-Benz Actros 2651 24V",
                PlacaSnapshot = "ACT-2026",
                ClienteId = Guid.NewGuid(),
                ClienteNomeSnapshot = "Transportadora Rápida",
                TecnicoId = 1,
                ProblemaRelatado = "Luz de falha elétrica no painel e perda intermitente de carga",
                DiagnosticoFinal = "Queda de 3,5V no cabo B+ do alternador",
                Status = "Concluida"
            };
            _repos.OrdensServico.Inserir(os);

            var caso = await _knowledgeService.CriarCasoAPartirDeOSAsync(
                os.Id,
                "Alimentação e Carga",
                "Cabo positivo de carga rompido na presilha do chassi",
                "Substituição do cabo 50mm² e isolamento térmico reforçado",
                medicoes: "Tensão alternador: 28,4V / Tensão bateria: 24,9V -> Queda 3,5V",
                pecasUtilizadas: "1 Cabo 50mm² 2,5m; 2 Terminais 50x10",
                dtcCodes: "P0562");

            Assert.NotNull(caso);
            Assert.StartsWith("CASO-", caso.Code);
            Assert.Equal("24V", caso.Voltage);
            Assert.Equal("Mercedes-Benz Actros 2651 24V", caso.VehicleModel);
            Assert.Equal("ACT-2026", caso.VehiclePlate);
            Assert.Equal(os.Id, caso.WorkOrderId);
            Assert.Equal("OS-2026-9999", caso.WorkOrderNumber);
            Assert.Equal(DiagnosticCaseResult.RESOLVED, caso.FinalResult);

            var casoNoBanco = await _knowledgeService.ObterCasoPorIdAsync(caso.CaseId);
            Assert.NotNull(casoNoBanco);
            Assert.Equal("Cabo positivo de carga rompido na presilha do chassi", casoNoBanco.ConfirmedCause);
        }

        [Fact]
        public async Task ListarCasosPorVeiculo_FiltraCasosDoVeiculoCorretamente()
        {
            var veiculoId1 = Guid.NewGuid();
            var veiculoId2 = Guid.NewGuid();

            var c1 = new DiagnosticCase
            {
                CaseId = Guid.NewGuid(),
                Code = "CASO-VEIC-01",
                Title = "Caso Veiculo 1",
                VehicleId = veiculoId1,
                VehicleModel = "VW Gol 1.6",
                ConfirmedCause = "Alternador",
                Solution = "Troca escovas",
                System = "Elétrica"
            };
            var c2 = new DiagnosticCase
            {
                CaseId = Guid.NewGuid(),
                Code = "CASO-VEIC-02",
                Title = "Caso Veiculo 2",
                VehicleId = veiculoId2,
                VehicleModel = "Fiat Strada",
                ConfirmedCause = "Bateria",
                Solution = "Troca bateria",
                System = "Carga"
            };

            await _knowledgeService.SalvarCasoAsync(c1);
            await _knowledgeService.SalvarCasoAsync(c2);

            var casosVeic1 = await _knowledgeService.ListarCasosPorVeiculoAsync(veiculoId1);
            Assert.Single(casosVeic1);
            Assert.Equal("CASO-VEIC-01", casosVeic1[0].Code);
        }
    }
}
