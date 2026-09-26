using System;
using System.Collections.Generic;
using System.Linq;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class DefeitosRecorrentesConsolidationTests
    {
        private static OrdemServico Os(string problema, DateTime abertura, string? peca = null, DateTime? garantiaAte = null, Guid? veiculoId = null, int tempoMin = 0)
        {
            var os = new OrdemServico
            {
                Id = Guid.NewGuid(),
                Ativo = true,
                ProblemaRelatado = problema,
                DataAbertura = abertura,
                VeiculoId = veiculoId,
                TempoRealMinutos = tempoMin,
                GarantiaValidaAte = garantiaAte,
                GarantiaObservacoes = garantiaAte.HasValue ? "Regulador alternador" : string.Empty
            };
            if (!string.IsNullOrWhiteSpace(peca))
            {
                os.Itens.Add(new OrdemServicoItem
                {
                    Id = Guid.NewGuid(),
                    Descricao = peca,
                    Quantidade = 1,
                    ValorUnitario = 100m
                });
            }
            return os;
        }

        [Fact]
        public void SemHistorico_RetornaListaVazia()
        {
            var resumo = AutoEletricaTecnicaService.ConsolidarDefeitosRecorrentes(
                Array.Empty<OrdemServico>(),
                new Dictionary<Guid, Veiculo>());
            Assert.Empty(resumo);
        }

        [Fact]
        public void UmHistorico_NaoEntraEmMesmoDefeito()
        {
            var os = Os("alternador sem carga", DateTime.Today.AddDays(-1));
            var resumo = AutoEletricaTecnicaService.ConsolidarDefeitosRecorrentes(
                new[] { os },
                new Dictionary<Guid, Veiculo>());
            Assert.DoesNotContain(resumo, r => r.Tipo == "Mesmo defeito");
        }

        [Fact]
        public void VariosHistoricos_MesmoDefeitoApareceComQuantidade2()
        {
            var a = Os("alternador sem carga", DateTime.Today.AddDays(-10));
            var b = Os("alternador sem carga", DateTime.Today.AddDays(-1));
            var resumo = AutoEletricaTecnicaService.ConsolidarDefeitosRecorrentes(
                new[] { a, b },
                new Dictionary<Guid, Veiculo>());
            var mesmo = Assert.Single(resumo.Where(r => r.Tipo == "Mesmo defeito"));
            Assert.Equal(2, mesmo.Quantidade);
            Assert.Contains("alternador", mesmo.Descricao, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RecenteComPoucos_NaoECortadoPorLegadoComTake5()
        {
            // Mesma quantidade (2) em 30 grupos legados + 1 recente: o ThenByDescending(DataAbertura)
            // + Take(25) mantem o retorno recente no painel (o antigo Take(5) o afogava).
            var ordens = new List<OrdemServico>();
            for (var i = 0; i < 30; i++)
            {
                var nome = $"defeito legado {i:D2}";
                ordens.Add(Os(nome, DateTime.Today.AddDays(-400 - i)));
                ordens.Add(Os(nome, DateTime.Today.AddDays(-390 - i)));
            }

            ordens.Add(Os("retorno recentissimo alternador", DateTime.Today.AddDays(-2)));
            ordens.Add(Os("retorno recentissimo alternador", DateTime.Today.AddDays(-1)));

            var resumo = AutoEletricaTecnicaService.ConsolidarDefeitosRecorrentes(
                ordens,
                new Dictionary<Guid, Veiculo>());

            Assert.Contains(
                resumo,
                r => r.Tipo == "Mesmo defeito"
                     && r.Quantidade == 2
                     && r.Descricao.Contains("retorno recentissimo", StringComparison.OrdinalIgnoreCase));
            Assert.True(
                resumo.Count(r => r.Tipo == "Mesmo defeito") > 5,
                "limite do painel deve expor mais que os antigos top-5");
            Assert.True(
                resumo.Count(r => r.Tipo == "Mesmo defeito") <= 25,
                "limite do painel Mesmo defeito nao deve exceder Take(25)");
        }

        [Fact]
        public void PecaGarantiaModelo_ConsolidadosQuandoPresentes()
        {
            var veiculoId = Guid.NewGuid();
            var veiculos = new Dictionary<Guid, Veiculo>
            {
                [veiculoId] = new Veiculo { Id = veiculoId, Marca = "VW", Modelo = "Gol" }
            };

            var a = Os("partida fraca", DateTime.Today.AddDays(-5), peca: "Regulador alternador", garantiaAte: DateTime.Today.AddDays(20), veiculoId: veiculoId, tempoMin: 90);
            var b = Os("partida fraca", DateTime.Today.AddDays(-1), peca: "Regulador alternador", garantiaAte: DateTime.Today.AddDays(10), veiculoId: veiculoId, tempoMin: 120);

            var resumo = AutoEletricaTecnicaService.ConsolidarDefeitosRecorrentes(new[] { a, b }, veiculos);

            Assert.Contains(resumo, r => r.Tipo == "Peca que mais falha" && r.Descricao.Contains("Regulador", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(resumo, r => r.Tipo == "Garantia ativa");
            Assert.Contains(resumo, r => r.Tipo == "Defeito por modelo" && r.Descricao.Contains("Gol", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(resumo, r => r.Tipo == "Tempo medio de resolucao" && r.TempoMedioMinutos > 0);
        }
    }
}
