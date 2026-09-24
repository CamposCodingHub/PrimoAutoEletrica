#nullable enable
using System;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class ChecklistTecnicoServiceTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly ChecklistTecnicoService _service;

        public ChecklistTecnicoServiceTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PrimoAuto_ChecklistTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _service = new ChecklistTecnicoService(_tempDir);
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
        public void SalvarChecklist_SemOrdemServicoId_LancaArgumentException()
        {
            var cl = new ChecklistTecnicoOS
            {
                OrdemServicoId = Guid.Empty,
                VeiculoId = Guid.NewGuid()
            };

            var ex = Assert.Throws<ArgumentException>(() => _service.Salvar(cl));
            Assert.Contains("OrdemServicoId", ex.Message);
        }

        [Fact]
        public void SalvarChecklist_SemVeiculoId_LancaArgumentException()
        {
            var cl = new ChecklistTecnicoOS
            {
                OrdemServicoId = Guid.NewGuid(),
                VeiculoId = Guid.Empty
            };

            var ex = Assert.Throws<ArgumentException>(() => _service.Salvar(cl));
            Assert.Contains("VeiculoId", ex.Message);
        }

        [Fact]
        public void ObterOuCriarPadrao_12V_CriaItensDeAutoEletricaLeve()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var cl = _service.ObterOuCriarPadrao(osId, veiculoId, isLinhaPesada: false);

            Assert.NotNull(cl);
            Assert.Equal(osId, cl.OrdemServicoId);
            Assert.Equal(veiculoId, cl.VeiculoId);
            Assert.Equal("12V", cl.ContextoTensao);
            Assert.True(cl.TotalItens >= 15);
            Assert.Contains(cl.Itens, i => i.Secao == "BATERIA");
            Assert.Contains(cl.Itens, i => i.Secao == "ALTERNADOR");
            Assert.Contains(cl.Itens, i => i.Secao == "PARTIDA");
            Assert.Contains(cl.Itens, i => i.Secao == "ATERRAMENTO");
            Assert.Contains(cl.Itens, i => i.Secao.Contains("FUSÍVEIS"));
            Assert.Contains(cl.Itens, i => i.Secao.Contains("ILUMINAÇÃO"));
        }

        [Fact]
        public void ObterOuCriarPadrao_24V_CriaItensLinhaPesadaHeavyDuty()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var cl = _service.ObterOuCriarPadrao(osId, veiculoId, isLinhaPesada: true);

            Assert.NotNull(cl);
            Assert.Equal("24V", cl.ContextoTensao);
            Assert.Contains(cl.Itens, i => i.Descricao.Contains("24V série"));
            Assert.Contains(cl.Itens, i => i.Descricao.Contains("Desbalanceamento"));
            Assert.Contains(cl.Itens, i => i.ItemId == "BAT_04");
            Assert.Contains(cl.Itens, i => i.ItemId == "BAT_05");
        }

        [Fact]
        public void MedicaoAntesEDepois_CalculaDeltaAutomatico()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var cl = _service.ObterOuCriarPadrao(osId, veiculoId, isLinhaPesada: false);
            var itemBateria = cl.Itens.First(i => i.Secao == "BATERIA");

            itemBateria.ValorMedido = 12.10m;
            itemBateria.Status = ChecklistStatusEnum.Atencao;
            itemBateria.ValorPosReparo = 12.65m;

            Assert.NotNull(itemBateria.DeltaPosReparo);
            Assert.Equal(0.55m, itemBateria.DeltaPosReparo!.Value);

            _service.Salvar(cl);

            var recarregado = _service.ObterPorOrdemServicoId(osId);
            Assert.NotNull(recarregado);
            var itemRecarregado = recarregado!.Itens.First(i => i.ItemId == itemBateria.ItemId);
            Assert.Equal(12.10m, itemRecarregado.ValorMedido);
            Assert.Equal(12.65m, itemRecarregado.ValorPosReparo);
            Assert.Equal(0.55m, itemRecarregado.DeltaPosReparo);
            Assert.Equal(ChecklistStatusEnum.Atencao, itemRecarregado.Status);
        }

        [Fact]
        public void StatusEstruturados_SuportaNaoDisponivelENaoAplicavel()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var cl = _service.ObterOuCriarPadrao(osId, veiculoId, isLinhaPesada: false);
            cl.Itens[0].Status = ChecklistStatusEnum.NaoDisponivel;
            cl.Itens[1].Status = ChecklistStatusEnum.NaoAplicavel;
            cl.Itens[2].Status = ChecklistStatusEnum.Critico;
            cl.Itens[3].Status = ChecklistStatusEnum.OK;

            _service.Salvar(cl);

            var recarregado = _service.ObterPorOrdemServicoId(osId);
            Assert.NotNull(recarregado);
            Assert.Equal(ChecklistStatusEnum.NaoDisponivel, recarregado!.Itens[0].Status);
            Assert.Equal(ChecklistStatusEnum.NaoAplicavel, recarregado.Itens[1].Status);
            Assert.Equal(ChecklistStatusEnum.Critico, recarregado.Itens[2].Status);
            Assert.Equal(ChecklistStatusEnum.OK, recarregado.Itens[3].Status);
            Assert.True(recarregado.TotalCritico >= 1);
            Assert.True(recarregado.TotalConforme >= 1);
        }

        [Fact]
        public void ConcluirChecklist_RegistraDataResponsavelEBloqueiaPendencias()
        {
            var osId = Guid.NewGuid();
            var veiculoId = Guid.NewGuid();

            var cl = _service.ObterOuCriarPadrao(osId, veiculoId, isLinhaPesada: false);
            _service.Salvar(cl);

            var sucesso = _service.Concluir(cl.Id, "Eletricista Chefe Roberto", "Revisão geral aprovada.");
            Assert.True(sucesso);

            var concluido = _service.ObterPorId(cl.Id);
            Assert.NotNull(concluido);
            Assert.True(concluido!.Concluido);
            Assert.NotNull(concluido.DataConclusao);
            Assert.Equal("Eletricista Chefe Roberto", concluido.TecnicoResponsavel);
            Assert.Equal("Revisão geral aprovada.", concluido.ObservacoesGerais);
        }

        [Fact]
        public void ListarPorVeiculoId_RetornaMultiplosChecklistsSemSobrescrever()
        {
            var veiculoId = Guid.NewGuid();
            var os1Id = Guid.NewGuid();
            var os2Id = Guid.NewGuid();

            var cl1 = _service.ObterOuCriarPadrao(os1Id, veiculoId, isLinhaPesada: false);
            cl1.ObservacoesGerais = "OS-1 Revisao Inicial";
            _service.Salvar(cl1);

            var cl2 = _service.ObterOuCriarPadrao(os2Id, veiculoId, isLinhaPesada: false);
            cl2.ObservacoesGerais = "OS-2 Retorno Preventivo";
            _service.Salvar(cl2);

            var checklistsVeiculo = _service.ListarPorVeiculoId(veiculoId);
            Assert.Equal(2, checklistsVeiculo.Count);
            Assert.Contains(checklistsVeiculo, c => c.OrdemServicoId == os1Id && c.ObservacoesGerais == "OS-1 Revisao Inicial");
            Assert.Contains(checklistsVeiculo, c => c.OrdemServicoId == os2Id && c.ObservacoesGerais == "OS-2 Retorno Preventivo");
        }
    }
}
