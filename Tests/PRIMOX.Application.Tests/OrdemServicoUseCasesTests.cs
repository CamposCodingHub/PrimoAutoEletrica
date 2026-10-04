using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PRIMOX.Application.DTOs;
using PRIMOX.Application.Interfaces;
using PRIMOX.Application.UseCases.OrdensServico;
using PRIMOX.Domain.Entities;
using PRIMOX.Domain.Enums;
using PRIMOX.Domain.Interfaces;
using PRIMOX.Infrastructure.Persistence;
using PRIMOX.Infrastructure.Time;
using Xunit;

namespace PRIMOX.Application.Tests
{
    public class OrdemServicoUseCasesTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly IOrdemServicoRepository _repository;
        private readonly ITimeProvider _timeProvider;

        public OrdemServicoUseCasesTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"primox_test_{Guid.NewGuid():N}.db");
            var connStr = $"Data Source={_testDbPath}";
            _repository = new SqliteOrdemServicoRepository(connStr);
            _timeProvider = DefaultTimeProvider.Instance;
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_testDbPath))
                {
                    File.Delete(_testDbPath);
                }
            }
            catch
            {
                // Limpeza de arquivo temporário
            }
        }

        [Fact]
        public async Task AbrirOrdemServico_Com_Dados_Validos_Deve_Salvar_E_Retornar_Sucesso()
        {
            var useCase = new AbrirOrdemServicoUseCase(_repository, _timeProvider);
            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var command = new AbrirOrdemServicoCommand(
                ClienteId: clienteId,
                VeiculoId: veiculoId,
                QueixaCliente: "Luz de injeção acesa e motor falhando em marcha lenta",
                Prioridade: "Alta",
                ClienteNome: "Carlos Mecânico",
                VeiculoPlaca: "ABC1D23",
                VeiculoModelo: "HB20 2014");

            var result = await useCase.ExecutarAsync(command);

            Assert.True(result.Sucesso);
            Assert.NotNull(result.OrdemServicoId);
            Assert.StartsWith("OS-", result.Numero);
            Assert.Equal("Aberta", result.Dados?.Status);

            // Verificar persistência
            var persistida = await _repository.ObterPorIdAsync(result.OrdemServicoId.Value);
            Assert.NotNull(persistida);
            Assert.Equal(clienteId, persistida.ClienteId);
            Assert.Equal(veiculoId, persistida.VeiculoId);
            Assert.Equal("Luz de injeção acesa e motor falhando em marcha lenta", persistida.QueixaCliente);
        }

        [Fact]
        public async Task AbrirOrdemServico_Sem_Cliente_Deve_Falhar_Com_Mensagem()
        {
            var useCase = new AbrirOrdemServicoUseCase(_repository, _timeProvider);

            var command = new AbrirOrdemServicoCommand(
                ClienteId: Guid.Empty,
                VeiculoId: Guid.NewGuid(),
                QueixaCliente: "Queixa de teste");

            var result = await useCase.ExecutarAsync(command);

            Assert.False(result.Sucesso);
            Assert.Contains("Cliente é obrigatório", result.MensagemErro);
        }

        [Fact]
        public async Task ObterOrdemServico_Existente_Deve_Retornar_Dto_Completo()
        {
            var abrirUseCase = new AbrirOrdemServicoUseCase(_repository, _timeProvider);
            var abrirResult = await abrirUseCase.ExecutarAsync(new AbrirOrdemServicoCommand(
                ClienteId: Guid.NewGuid(),
                VeiculoId: Guid.NewGuid(),
                QueixaCliente: "Troca preventiva de velas",
                ClienteNome: "João Silva",
                VeiculoPlaca: "XYZ9999",
                VeiculoModelo: "Gol G5 2010"));

            var obterUseCase = new ObterOrdemServicoUseCase(_repository);
            var queryResult = await obterUseCase.ExecutarAsync(new ObterOrdemServicoQuery(abrirResult.OrdemServicoId!.Value));

            Assert.True(queryResult.Sucesso);
            Assert.NotNull(queryResult.Dados);
            Assert.Equal("João Silva", queryResult.Dados.ClienteNome);
            Assert.Equal("XYZ9999", queryResult.Dados.VeiculoPlaca);
        }

        [Fact]
        public async Task AdicionarItemPeca_E_ItemServico_Deve_Atualizar_Total_Na_Persistencia()
        {
            var abrirUseCase = new AbrirOrdemServicoUseCase(_repository, _timeProvider);
            var osResult = await abrirUseCase.ExecutarAsync(new AbrirOrdemServicoCommand(
                ClienteId: Guid.NewGuid(),
                VeiculoId: Guid.NewGuid(),
                QueixaCliente: "Alternador ruidoso e cheiro de queimado"));

            var osId = osResult.OrdemServicoId!.Value;

            var pecaUseCase = new AdicionarItemPecaUseCase(_repository);
            var pecaResult = await pecaUseCase.ExecutarAsync(new AdicionarItemPecaCommand(
                OrdemServicoId: osId,
                Descricao: "Rolamento do Alternador 6203",
                Quantidade: 2,
                ValorUnitario: 45.00m,
                Codigo: "ROL-6203"));

            Assert.True(pecaResult.Sucesso);
            Assert.NotNull(pecaResult.ItemId);

            var servicoUseCase = new AdicionarItemServicoUseCase(_repository);
            var servResult = await servicoUseCase.ExecutarAsync(new AdicionarItemServicoCommand(
                OrdemServicoId: osId,
                Descricao: "Desmontagem e Troca de Rolamentos",
                QuantidadeHoras: 1.5m,
                ValorHora: 100.00m,
                TecnicoResponsavel: "Pedro Técnico"));

            Assert.True(servResult.Sucesso);

            // Verificar no repositório persistido
            var osPersistida = await _repository.ObterPorIdAsync(osId);
            Assert.NotNull(osPersistida);
            Assert.Single(osPersistida.ItensPeca);
            Assert.Single(osPersistida.ItensServico);

            // Total Peças: 2 * 45 = 90
            Assert.Equal(90.00m, osPersistida.TotalPecas.Amount);
            // Total Serviços: 1.5 * 100 = 150
            Assert.Equal(150.00m, osPersistida.TotalServicos.Amount);
            // Total Bruto: 240
            Assert.Equal(240.00m, osPersistida.TotalBruto.Amount);
        }

        [Fact]
        public async Task AlterarStatus_Valido_Deve_Atualizar_Status_E_Historico()
        {
            var abrirUseCase = new AbrirOrdemServicoUseCase(_repository, _timeProvider);
            var osResult = await abrirUseCase.ExecutarAsync(new AbrirOrdemServicoCommand(
                ClienteId: Guid.NewGuid(),
                VeiculoId: Guid.NewGuid(),
                QueixaCliente: "Revisão geral"));

            var osId = osResult.OrdemServicoId!.Value;

            var alterarStatusUseCase = new AlterarStatusOrdemServicoUseCase(_repository, _timeProvider);
            var result = await alterarStatusUseCase.ExecutarAsync(new AlterarStatusOrdemServicoCommand(
                OrdemServicoId: osId,
                NovoStatus: "EmTriagemDvi",
                Motivo: "Encaminhado para o box de inspeção semafórica",
                Responsavel: "Inspetor Paulo"));

            Assert.True(result.Sucesso);
            Assert.Equal("Aberta", result.StatusAnterior);
            Assert.Equal("EmTriagemDvi", result.StatusAtual);

            var osPersistida = await _repository.ObterPorIdAsync(osId);
            Assert.NotNull(osPersistida);
            Assert.Equal(StatusOrdemServico.EmTriagemDvi, osPersistida.Status);
            Assert.Equal(2, osPersistida.Historico.Count);
        }

        [Fact]
        public async Task AlterarStatus_Invalido_Deve_Retornar_Falha_De_Transicao()
        {
            var abrirUseCase = new AbrirOrdemServicoUseCase(_repository, _timeProvider);
            var osResult = await abrirUseCase.ExecutarAsync(new AbrirOrdemServicoCommand(
                ClienteId: Guid.NewGuid(),
                VeiculoId: Guid.NewGuid(),
                QueixaCliente: "Revisão geral"));

            var osId = osResult.OrdemServicoId!.Value;

            var alterarStatusUseCase = new AlterarStatusOrdemServicoUseCase(_repository, _timeProvider);
            // Pular direto de Aberta para Finalizada deve falhar pela máquina de estados do domínio
            var result = await alterarStatusUseCase.ExecutarAsync(new AlterarStatusOrdemServicoCommand(
                OrdemServicoId: osId,
                NovoStatus: "Finalizada",
                Motivo: "Tentativa de fechamento direto",
                Responsavel: "Admin"));

            Assert.False(result.Sucesso);
            Assert.Contains("Transição de status inválida", result.MensagemErro);
        }
    }
}
