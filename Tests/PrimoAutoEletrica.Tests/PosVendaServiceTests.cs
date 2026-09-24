using System;
using System.IO;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public sealed class PosVendaServiceTests : IDisposable
    {
        private readonly string _testDir;
        private readonly PosVendaService _service;

        public PosVendaServiceTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "primox_posvenda_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDir);
            _service = new PosVendaService(_testDir);
        }

        public void Dispose()
        {
            PosVendaService.TestStorageDirectoryOverride = null;
            if (Directory.Exists(_testDir))
            {
                try { Directory.Delete(_testDir, true); } catch { }
            }
        }

        [Fact]
        public void Salvar_E_ObterPorId_DevePersistirComSucesso()
        {
            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();
            var osId = Guid.NewGuid();

            var item = new PosVendaItem
            {
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                OrdemServicoId = osId,
                Tipo = PosVendaTipoEnum.RevisaoPreventiva,
                Status = PosVendaStatusEnum.Pendente,
                Observacoes = "Revisao de 6 meses"
            };

            var salvo = _service.Salvar(item);
            Assert.NotNull(salvo);
            Assert.NotEqual(Guid.Empty, salvo.Id);

            var recuperado = _service.ObterPorId(salvo.Id);
            Assert.NotNull(recuperado);
            Assert.Equal(clienteId, recuperado!.ClienteId);
            Assert.Equal(veiculoId, recuperado.VeiculoId);
            Assert.Equal(osId, recuperado.OrdemServicoId);
            Assert.Equal(PosVendaTipoEnum.RevisaoPreventiva, recuperado.Tipo);
        }

        [Fact]
        public void ListarPorClienteId_DeveFiltrarCorretamentePorId()
        {
            var clienteA = Guid.NewGuid();
            var clienteB = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            _service.Salvar(new PosVendaItem { ClienteId = clienteA, VeiculoId = veiculoId, Tipo = PosVendaTipoEnum.FollowUpPosServico });
            _service.Salvar(new PosVendaItem { ClienteId = clienteA, VeiculoId = veiculoId, Tipo = PosVendaTipoEnum.Garantia });
            _service.Salvar(new PosVendaItem { ClienteId = clienteB, VeiculoId = veiculoId, Tipo = PosVendaTipoEnum.Retorno });

            var itensA = _service.ListarPorClienteId(clienteA);
            var itensB = _service.ListarPorClienteId(clienteB);

            Assert.Equal(2, itensA.Count);
            Assert.Single(itensB);
            Assert.All(itensA, x => Assert.Equal(clienteA, x.ClienteId));
            Assert.All(itensB, x => Assert.Equal(clienteB, x.ClienteId));
        }

        [Fact]
        public void CriarDeOrdemServico_DevePreservarIdsEConfigurarStatusPendente()
        {
            var clienteId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();
            var osId = Guid.NewGuid();

            var os = new OrdemServico
            {
                Id = osId,
                ClienteId = clienteId,
                VeiculoId = veiculoId,
                Numero = "OS-2026-0042",
                ClienteNomeSnapshot = "Carlos Eletrica",
                VeiculoDescricaoSnapshot = "Volvo FH 540",
                PlacaSnapshot = "MLB9J14",
                TecnicoId = 7,
                GarantiaValidaAte = DateTime.Today.AddDays(90)
            };

            var posVenda = _service.CriarDeOrdemServico(os, PosVendaTipoEnum.Garantia, dias: 30, responsavel: "Roberto Tech");

            Assert.NotNull(posVenda);
            Assert.Equal(osId, posVenda.OrdemServicoId);
            Assert.Equal(clienteId, posVenda.ClienteId);
            Assert.Equal(veiculoId, posVenda.VeiculoId);
            Assert.Equal(PosVendaTipoEnum.Garantia, posVenda.Tipo);
            Assert.Equal(PosVendaStatusEnum.Pendente, posVenda.Status);
            Assert.Equal("Roberto Tech", posVenda.Responsavel);
            Assert.Equal(DateTime.Today.AddDays(30), posVenda.DataPrevistaContato.Date);
            Assert.False(posVenda.ContatoRealizado);
        }

        [Fact]
        public void RegistrarContato_DeveMarcarResolvidoEAtualizarResultado()
        {
            var item = _service.Salvar(new PosVendaItem
            {
                ClienteId = Guid.NewGuid(),
                VeiculoId = Guid.NewGuid(),
                Tipo = PosVendaTipoEnum.FollowUpPosServico,
                Status = PosVendaStatusEnum.Pendente
            });

            var ok = _service.RegistrarContato(item.Id, "Cliente confirmou que alternador esta funcionando 100%.", "Atendente Maria", resolvido: true);

            Assert.True(ok);
            var atualizado = _service.ObterPorId(item.Id);
            Assert.NotNull(atualizado);
            Assert.True(atualizado!.Resolvido);
            Assert.True(atualizado.ContatoRealizado);
            Assert.Equal(PosVendaStatusEnum.Concluido, atualizado.Status);
            Assert.Equal("Atendente Maria", atualizado.Responsavel);
            Assert.NotNull(atualizado.DataContatoRealizado);
        }

        [Fact]
        public void Validacao_ExigeClienteIdEVeiculoIdValidos()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                _service.Salvar(new PosVendaItem { ClienteId = Guid.Empty, VeiculoId = Guid.NewGuid() });
            });

            Assert.Throws<ArgumentException>(() =>
            {
                _service.Salvar(new PosVendaItem { ClienteId = Guid.NewGuid(), VeiculoId = Guid.Empty });
            });
        }
    }
}
