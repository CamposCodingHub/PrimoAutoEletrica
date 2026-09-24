#nullable enable
using System;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class PosVendaServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly PosVendaService _service;

        public PosVendaServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PrimoAuto_PosVendaTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _service = new PosVendaService(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, true);
                }
            }
            catch
            {
                // Limpeza segura
            }
        }

        [Fact]
        public void Salvar_SemClienteId_LancaArgumentException()
        {
            var item = new PosVendaItem
            {
                ClienteId = Guid.Empty,
                VeiculoId = Guid.NewGuid(),
                OrdemServicoId = Guid.NewGuid()
            };

            var ex = Assert.Throws<ArgumentException>(() => _service.Salvar(item));
            Assert.Contains("ClienteId", ex.Message);
        }

        [Fact]
        public void Salvar_CriacaoEAtualizacao_PersisteCorretamente()
        {
            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();
            var osId = Guid.NewGuid();

            var item = new PosVendaItem
            {
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                OrdemServicoId = osId,
                Tipo = PosVendaTipoEnum.FollowUpPosServico,
                Status = PosVendaStatusEnum.Pendente,
                DataPrevistaContato = DateTime.Today.AddDays(3),
                Responsavel = "Consultor Lucas",
                Observacoes = "Acompanhamento pós troca de alternador."
            };

            var salvo = _service.Salvar(item);
            Assert.NotEqual(Guid.Empty, salvo.Id);

            var recuperado = _service.ObterPorId(salvo.Id);
            Assert.NotNull(recuperado);
            Assert.Equal(clienteId, recuperado!.ClienteId);
            Assert.Equal(veiculoId, recuperado.VeiculoId);
            Assert.Equal(osId, recuperado.OrdemServicoId);
            Assert.Equal(PosVendaTipoEnum.FollowUpPosServico, recuperado.Tipo);
            Assert.Equal(PosVendaStatusEnum.Pendente, recuperado.Status);
            Assert.Equal("Consultor Lucas", recuperado.Responsavel);
        }

        [Fact]
        public void RegistrarContato_AtualizaStatusEDataComResolucao()
        {
            var clienteId = Guid.NewGuid();
            var item = new PosVendaItem
            {
                ClienteId = clienteId,
                Tipo = PosVendaTipoEnum.Garantia,
                Status = PosVendaStatusEnum.Contatado,
                Responsavel = "Recepção",
                Observacoes = "Cliente informou chiado na correia."
            };

            var salvo = _service.Salvar(item);
            var sucesso = _service.RegistrarContato(
                salvo.Id,
                resultado: "Cliente confirmou agendamento para tensionamento.",
                responsavel: "Consultor Técnico Carlos",
                resolvido: true,
                novoStatus: PosVendaStatusEnum.Concluido);

            Assert.True(sucesso);

            var atualizado = _service.ObterPorId(salvo.Id);
            Assert.NotNull(atualizado);
            Assert.Equal(PosVendaStatusEnum.Concluido, atualizado!.Status);
            Assert.True(atualizado.Resolvido);
            Assert.NotNull(atualizado.DataContatoRealizado);
            Assert.Equal("Cliente confirmou agendamento para tensionamento.", atualizado.Resultado);
            Assert.Equal("Consultor Técnico Carlos", atualizado.Responsavel);
        }

        [Fact]
        public void HistoricoAcumulativo_MesmoClienteMesmoVeiculoDuasOS_MantemAmbosSeparados()
        {
            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();
            var osA = Guid.NewGuid();
            var osB = Guid.NewGuid();

            var posA = new PosVendaItem
            {
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                OrdemServicoId = osA,
                Tipo = PosVendaTipoEnum.FollowUpPosServico,
                Status = PosVendaStatusEnum.Concluido,
                Observacoes = "OS-A: Motor de partida revisado."
            };
            _service.Salvar(posA);

            var posB = new PosVendaItem
            {
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                OrdemServicoId = osB,
                Tipo = PosVendaTipoEnum.Retorno,
                Status = PosVendaStatusEnum.Pendente,
                Observacoes = "OS-B: Bateria descarregando (retorno preventivo)."
            };
            _service.Salvar(posB);

            // Verificar por Veículo
            var doVeiculo = _service.ListarPorVeiculoId(veiculoId);
            Assert.Equal(2, doVeiculo.Count);
            Assert.Contains(doVeiculo, i => i.OrdemServicoId == osA && i.Observacoes.Contains("OS-A"));
            Assert.Contains(doVeiculo, i => i.OrdemServicoId == osB && i.Observacoes.Contains("OS-B"));

            // Verificar por Cliente
            var doCliente = _service.ListarPorClienteId(clienteId);
            Assert.Equal(2, doCliente.Count);

            // Verificar por OS específica
            var daOsA = _service.ListarPorOrdemServicoId(osA);
            Assert.Single(daOsA);
            Assert.Equal("OS-A: Motor de partida revisado.", daOsA[0].Observacoes);

            var daOsB = _service.ListarPorOrdemServicoId(osB);
            Assert.Single(daOsB);
            Assert.Equal("OS-B: Bateria descarregando (retorno preventivo).", daOsB[0].Observacoes);
        }

        [Fact]
        public void FiltrosPendentesEVencidos_OperamCorretamente()
        {
            var clienteId = Guid.NewGuid();

            var pendenteFuturo = new PosVendaItem
            {
                ClienteId = clienteId,
                Status = PosVendaStatusEnum.Pendente,
                DataPrevistaContato = DateTime.Today.AddDays(5)
            };
            _service.Salvar(pendenteFuturo);

            var pendenteVencido = new PosVendaItem
            {
                ClienteId = clienteId,
                Status = PosVendaStatusEnum.Pendente,
                DataPrevistaContato = DateTime.Today.AddDays(-2)
            };
            _service.Salvar(pendenteVencido);

            var concluido = new PosVendaItem
            {
                ClienteId = clienteId,
                Status = PosVendaStatusEnum.Concluido,
                DataPrevistaContato = DateTime.Today.AddDays(-10)
            };
            _service.Salvar(concluido);

            var pendentes = _service.ListarPendentes();
            Assert.Equal(2, pendentes.Count);

            var vencidos = _service.ListarVencidos();
            Assert.Single(vencidos);
            Assert.Equal(pendenteVencido.Id, vencidos[0].Id);
        }
    }
}
