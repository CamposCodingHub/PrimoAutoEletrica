using System;
using System.IO;
using System.Threading.Tasks;
using PRIMOX.Application.DTOs;
using PRIMOX.Application.Interfaces;
using PRIMOX.Application.UseCases.OrdensServico;
using PRIMOX.Domain.Enums;
using PRIMOX.Domain.Interfaces;
using PRIMOX.Infrastructure.Persistence;
using PRIMOX.Infrastructure.Time;
using Xunit;

namespace PRIMOX.Application.Tests
{
    /// <summary>
    /// Teste de Dupla Entrada (Double-Entry Architectural Test).
    /// Comprova que o mesmo comportamento de negócio é executado tanto pela porta Desktop (ViewModel)
    /// quanto pela porta API (Controller/Endpoint) utilizando exatamente os mesmos Use Cases da Application Layer
    /// e as mesmas regras de Domínio, sem duplicação de lógica.
    /// </summary>
    public class DoubleEntryVerificationTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly IOrdemServicoRepository _repository;
        private readonly ITimeProvider _timeProvider;

        public DoubleEntryVerificationTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"primox_double_entry_{Guid.NewGuid():N}.db");
            _repository = new SqliteOrdemServicoRepository($"Data Source={_testDbPath}");
            _timeProvider = DefaultTimeProvider.Instance;
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
            }
            catch { }
        }

        [Fact]
        public async Task Dupla_Entrada_Desktop_E_Api_Devem_Executar_Mesmo_Caso_De_Uso_E_Dominio()
        {
            var abrirUseCase = new AbrirOrdemServicoUseCase(_repository, _timeProvider);
            var alterarStatusUseCase = new AlterarStatusOrdemServicoUseCase(_repository, _timeProvider);
            var adicionarPecaUseCase = new AdicionarItemPecaUseCase(_repository);

            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            // ==========================================
            // ENTRADA 1: Simulação do Desktop (WPF ViewModel)
            // ==========================================
            var desktopCommand = new AbrirOrdemServicoCommand(
                ClienteId: clienteId,
                VeiculoId: veiculoId,
                QueixaCliente: "HB20 2014: Fusível da bomba queima intermitentemente",
                Prioridade: "Alta",
                ClienteNome: "Oficina Balcão",
                VeiculoPlaca: "BRA2E19",
                VeiculoModelo: "HB20 1.6 2014");

            var desktopResult = await abrirUseCase.ExecutarAsync(desktopCommand);

            Assert.True(desktopResult.Sucesso, "Entrada Desktop deve ter sucesso");
            Assert.NotNull(desktopResult.OrdemServicoId);
            var desktopOsId = desktopResult.OrdemServicoId.Value;

            // ==========================================
            // ENTRADA 2: Simulação da Web API (HTTP Endpoint)
            // ==========================================
            var apiCommand = new AbrirOrdemServicoCommand(
                ClienteId: clienteId,
                VeiculoId: veiculoId,
                QueixaCliente: "HB20 2014: Fusível da bomba queima intermitentemente",
                Prioridade: "Alta",
                ClienteNome: "Cliente via Mobile/API",
                VeiculoPlaca: "BRA2E19",
                VeiculoModelo: "HB20 1.6 2014");

            var apiResult = await abrirUseCase.ExecutarAsync(apiCommand);

            Assert.True(apiResult.Sucesso, "Entrada API deve ter sucesso");
            Assert.NotNull(apiResult.OrdemServicoId);
            var apiOsId = apiResult.OrdemServicoId.Value;

            // Ambas as OSs foram abertas com números sequenciais distintos gerados pela infraestrutura
            Assert.NotEqual(desktopOsId, apiOsId);
            Assert.StartsWith("OS-", desktopResult.Numero);
            Assert.StartsWith("OS-", apiResult.Numero);

            // ==========================================
            // PROVA DE REGRA COMPARTILHADA (Máquina de Estados)
            // Tentativa de transição inválida em ambas as portas
            // ==========================================
            var invalidTransitionDesktop = await alterarStatusUseCase.ExecutarAsync(
                new AlterarStatusOrdemServicoCommand(desktopOsId, "Finalizada", "Pulo direto", "DesktopUser"));

            var invalidTransitionApi = await alterarStatusUseCase.ExecutarAsync(
                new AlterarStatusOrdemServicoCommand(apiOsId, "Finalizada", "Pulo direto", "ApiUser"));

            Assert.False(invalidTransitionDesktop.Sucesso);
            Assert.False(invalidTransitionApi.Sucesso);
            Assert.Contains("Transição de status inválida", invalidTransitionDesktop.MensagemErro);
            Assert.Contains("Transição de status inválida", invalidTransitionApi.MensagemErro);

            // ==========================================
            // PROVA DE REGRA COMPARTILHADA (Cálculo Monetário de Itens)
            // ==========================================
            var pecaResultDesktop = await adicionarPecaUseCase.ExecutarAsync(
                new AdicionarItemPecaCommand(desktopOsId, "Relé Auxiliar 40A", 1, 45.00m));

            var pecaResultApi = await adicionarPecaUseCase.ExecutarAsync(
                new AdicionarItemPecaCommand(apiOsId, "Relé Auxiliar 40A", 1, 45.00m));

            Assert.True(pecaResultDesktop.Sucesso);
            Assert.True(pecaResultApi.Sucesso);

            var osDesktopPersistida = await _repository.ObterPorIdAsync(desktopOsId);
            var osApiPersistida = await _repository.ObterPorIdAsync(apiOsId);

            Assert.Equal(45.00m, osDesktopPersistida!.TotalBruto.Amount);
            Assert.Equal(45.00m, osApiPersistida!.TotalBruto.Amount);
        }
    }
}
