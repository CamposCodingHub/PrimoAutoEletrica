using System;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class DviOrcamentoOsInheritanceTests : IDisposable
    {
        private readonly string _tempRoot;

        public DviOrcamentoOsInheritanceTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "primox-dvi-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
            DviChecklistService.TestAppDataOverride = _tempRoot;
        }

        public void Dispose()
        {
            DviChecklistService.TestAppDataOverride = null;
            try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public void SalvarSomenteOrcamento_DepoisCarregarOuPadrao_SemOs_HerdaItensEstadoObs()
        {
            var orcId = Guid.NewGuid();
            var svc = new DviChecklistService();
            var itens = new[]
            {
                new DviChecklistItem
                {
                    Categoria = "Bateria",
                    Nome = "Tensao",
                    OkEntrada = true,
                    OkSaida = false,
                    Observacoes = "12.4V",
                    FotoPath = "foto-bateria.jpg"
                },
                new DviChecklistItem
                {
                    Categoria = "Farol",
                    Nome = "Esquerdo",
                    OkEntrada = false,
                    OkSaida = false,
                    Observacoes = "quebrado"
                }
            };

            svc.SalvarSomenteOrcamento(orcId, itens);
            Assert.True(svc.ExisteParaOrcamento(orcId));

            var loaded = svc.CarregarOuPadrao(ordemId: null, orcamentoId: orcId);
            Assert.Equal(2, loaded.Count);
            Assert.Contains(loaded, x => x.Nome == "Tensao" && x.OkEntrada && x.Observacoes == "12.4V" && x.FotoPath == "foto-bateria.jpg");
            Assert.Contains(loaded, x => x.Nome == "Esquerdo" && !x.OkEntrada && x.Observacoes == "quebrado");
        }

        [Fact]
        public void QuandoOsJaTemDvi_NaoSubstituiPeloOrcamento()
        {
            var ordemId = Guid.NewGuid();
            var orcId = Guid.NewGuid();
            var svc = new DviChecklistService();

            svc.SalvarSomenteOrcamento(orcId, new[]
            {
                new DviChecklistItem { Categoria = "Orc", Nome = "DoOrcamento", OkEntrada = true }
            });
            svc.Salvar(ordemId, new[]
            {
                new DviChecklistItem { Categoria = "Os", Nome = "DaOs", OkEntrada = false, Observacoes = "os-wins" }
            }, orcamentoId: orcId);

            var loaded = svc.CarregarOuPadrao(ordemId, orcId);
            Assert.Single(loaded);
            Assert.Equal("DaOs", loaded[0].Nome);
            Assert.Equal("os-wins", loaded[0].Observacoes);
            Assert.DoesNotContain(loaded, x => x.Nome == "DoOrcamento");
        }

        [Fact]
        public void ReabrirOrcamento_NaoDuplicaItens()
        {
            var orcId = Guid.NewGuid();
            var svc = new DviChecklistService();
            var itens = svc.CriarPadrao().Take(3).ToList();
            itens[0].OkEntrada = true;
            itens[0].Observacoes = "ok";

            svc.SalvarSomenteOrcamento(orcId, itens);
            var a = svc.CarregarPorOrcamentoOuPadrao(orcId);
            svc.SalvarSomenteOrcamento(orcId, a);
            var b = svc.CarregarPorOrcamentoOuPadrao(orcId);

            Assert.Equal(a.Count, b.Count);
            Assert.Equal(3, b.Count);
            Assert.Equal(1, b.Count(x => x.OkEntrada));
        }
    }
}
