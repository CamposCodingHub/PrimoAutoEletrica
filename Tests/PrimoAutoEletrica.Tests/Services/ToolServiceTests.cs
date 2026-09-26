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
    public sealed class ToolServiceTests : IDisposable
    {
        private readonly string _testDir;
        private readonly DatabaseService _database;
        private readonly LoggerService _logger;
        private readonly RepositoryRegistry _repos;
        private readonly AppSessionService _session;
        private readonly AuditLogService _audit;
        private readonly PermissionService _permissionService;
        private readonly ToolService _toolService;
        private readonly Funcionario _adminUser;

        public ToolServiceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"ToolTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _logger = new LoggerService(_testDir);
            _database = new DatabaseService(_testDir, logger: _logger);
            _repos = new RepositoryRegistry(_database, _logger);
            _session = new AppSessionService();
            _audit = new AuditLogService(_database, _logger, _session);

            _adminUser = new Funcionario
            {
                Id = 1,
                Nome = "Administrador Teste",
                Email = "admin@primoauto.com",
                PerfilAcesso = "Administrador"
            };
            _session.StartSession(_adminUser);
            _permissionService = new PermissionService(_adminUser, _logger);

            _toolService = new ToolService(
                _repos.Tools,
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
        public async Task ListarFerramentas_RetornaFerramentasIniciaisDoSeed()
        {
            var ferramentas = await _toolService.ListarFerramentasAsync();
            Assert.NotNull(ferramentas);
            Assert.True(ferramentas.Count >= 6, "Deveria conter as 6 ferramentas iniciais do seed.");
            Assert.Contains(ferramentas, f => f.Code == "F001");
            Assert.Contains(ferramentas, f => f.Code == "F004");
        }

        [Fact]
        public async Task CriarFerramenta_ComSucesso_SalvaNoBancoComCentsV1()
        {
            var tool = new Tool
            {
                ToolId = Guid.NewGuid(),
                Code = "TEST-01",
                Name = "Alicate de Pressão Especial",
                Category = "Mecânica",
                Brand = "Gedore",
                Model = "137-10",
                LocationName = "Gaveta 03",
                Status = ToolStatus.AVAILABLE,
                PurchaseValue = 189.90m // 18990 cents
            };

            var salvo = await _toolService.SalvarFerramentaAsync(tool);
            Assert.True(salvo);

            var recuperada = await _toolService.ObterFerramentaPorCodigoAsync("TEST-01");
            Assert.NotNull(recuperada);
            Assert.Equal("Alicate de Pressão Especial", recuperada.Name);
            Assert.Equal(18990, recuperada.PurchaseValueCents);
            Assert.Equal(189.90m, recuperada.PurchaseValue);
        }

        [Fact]
        public async Task RetirarFerramenta_FerramentaDisponivel_MudaStatusParaInUse()
        {
            var tool = await _toolService.ObterFerramentaPorCodigoAsync("F001");
            Assert.NotNull(tool);
            Assert.Equal(ToolStatus.AVAILABLE, tool.Status);

            var retirado = await _toolService.RetirarFerramentaAsync(
                tool.ToolId,
                1,
                "Carlos Eletricista",
                null,
                "OS-1001",
                "ABC-1234",
                DateTime.Now.AddHours(4),
                "Diagnóstico elétrico de alternador");

            Assert.True(retirado);

            var toolAtualizada = await _toolService.ObterFerramentaPorIdAsync(tool.ToolId);
            Assert.NotNull(toolAtualizada);
            Assert.Equal(ToolStatus.IN_USE, toolAtualizada.Status);
            Assert.Equal(1, toolAtualizada.CurrentResponsibleUserId);
            Assert.Equal("Carlos Eletricista", toolAtualizada.CurrentResponsibleUserName);

            var checkoutAtivo = await _toolService.ObterCheckoutAtivoAsync(tool.ToolId);
            Assert.NotNull(checkoutAtivo);
            Assert.Equal("OS-1001", checkoutAtivo.WorkOrderNumber);
            Assert.Equal(ToolCheckoutStatus.OPEN, checkoutAtivo.Status);
        }

        [Fact]
        public async Task RetirarFerramenta_FerramentaJaEmUso_LancaInvalidOperationException()
        {
            var tool = await _toolService.ObterFerramentaPorCodigoAsync("F002");
            Assert.NotNull(tool);

            // Primeira retirada
            await _toolService.RetirarFerramentaAsync(tool.ToolId, 1, "Técnico 1");

            // Segunda tentativa concorrente deve falhar
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _toolService.RetirarFerramentaAsync(tool.ToolId, 2, "Técnico 2"));

            Assert.Contains("não está disponível", ex.Message);
        }

        [Fact]
        public async Task DevolverFerramenta_CondicaoOk_RetornaParaAvailable()
        {
            var tool = await _toolService.ObterFerramentaPorCodigoAsync("F003");
            Assert.NotNull(tool);

            await _toolService.RetirarFerramentaAsync(tool.ToolId, 1, "Técnico Diagnóstico");
            var devolvido = await _toolService.DevolverFerramentaAsync(tool.ToolId, 1, ToolCondition.OK, "Sem avarias.");

            Assert.True(devolvido);

            var toolAtualizada = await _toolService.ObterFerramentaPorIdAsync(tool.ToolId);
            Assert.NotNull(toolAtualizada);
            Assert.Equal(ToolStatus.AVAILABLE, toolAtualizada.Status);
            Assert.Null(toolAtualizada.CurrentResponsibleUserId);
        }

        [Fact]
        public async Task DevolverFerramenta_CondicaoDanificada_MudaStatusParaDamaged()
        {
            var tool = await _toolService.ObterFerramentaPorCodigoAsync("F004");
            Assert.NotNull(tool);

            await _toolService.RetirarFerramentaAsync(tool.ToolId, 1, "Técnico Osciloscópio");
            var devolvido = await _toolService.DevolverFerramentaAsync(tool.ToolId, 1, ToolCondition.DAMAGED, "Cabo do canal 1 com mau contato.");

            Assert.True(devolvido);

            var toolAtualizada = await _toolService.ObterFerramentaPorIdAsync(tool.ToolId);
            Assert.NotNull(toolAtualizada);
            Assert.Equal(ToolStatus.DAMAGED, toolAtualizada.Status);
        }

        [Fact]
        public async Task DevolverFerramenta_CondicaoNecessitaCalibracao_MudaStatusParaMaintenance()
        {
            var tool = await _toolService.ObterFerramentaPorCodigoAsync("F005");
            Assert.NotNull(tool);

            await _toolService.RetirarFerramentaAsync(tool.ToolId, 1, "Técnico Fonte");
            var devolvido = await _toolService.DevolverFerramentaAsync(tool.ToolId, 1, ToolCondition.NEEDS_CALIBRATION, "Aferição anual de tensão.");

            Assert.True(devolvido);

            var toolAtualizada = await _toolService.ObterFerramentaPorIdAsync(tool.ToolId);
            Assert.NotNull(toolAtualizada);
            Assert.Equal(ToolStatus.MAINTENANCE, toolAtualizada.Status);
        }

        [Fact]
        public async Task RegistrarManutencao_GravaCustoCentsV1EAtualizaUltimaManutencao()
        {
            var tool = await _toolService.ObterFerramentaPorCodigoAsync("F006");
            Assert.NotNull(tool);

            var sucesso = await _toolService.RegistrarManutencaoAsync(
                tool.ToolId,
                ToolMaintenanceType.CALIBRATION,
                "Calibração de condutância e substituição de garras jacaré",
                250.00m,
                "Laboratório Especializado SP",
                "Certificado metrológico RBC 109238");

            Assert.True(sucesso);

            var manutencoes = await _toolService.ObterManutencoesAsync(tool.ToolId);
            Assert.Single(manutencoes);
            Assert.Equal(25000, manutencoes[0].CostCents);
            Assert.Equal(250.00m, manutencoes[0].Cost);
            Assert.Equal(ToolMaintenanceType.CALIBRATION, manutencoes[0].MaintenanceType);
        }

        [Fact]
        public async Task ExcluirFerramenta_EmUso_LancaInvalidOperationException()
        {
            var tool = await _toolService.ObterFerramentaPorCodigoAsync("F001");
            Assert.NotNull(tool);

            await _toolService.RetirarFerramentaAsync(tool.ToolId, 1, "Técnico");

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _toolService.ExcluirFerramentaAsync(tool.ToolId));

            Assert.Contains("está atualmente em uso", ex.Message);
        }
    }
}
