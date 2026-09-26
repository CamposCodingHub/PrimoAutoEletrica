using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class LucroPorServicoMathTests
    {
        public static IEnumerable<object[]> CentEdgeCases()
        {
            // receita, custo, lucro esperado (decimal)
            yield return new object[] { 100.00m, 40.00m, 60.00m };
            yield return new object[] { 0.01m, 0.00m, 0.01m };
            yield return new object[] { 0.05m, 0.01m, 0.04m };
            yield return new object[] { 0.10m, 0.05m, 0.05m };
            yield return new object[] { 0.99m, 0.50m, 0.49m };
            yield return new object[] { 1.01m, 0.01m, 1.00m };
            yield return new object[] { 10.99m, 4.50m, 6.49m };
            yield return new object[] { 100.50m, 25.25m, 75.25m };
            yield return new object[] { 1000.99m, 500.50m, 500.49m };
            yield return new object[] { 10000.99m, 1.00m, 9999.99m };
        }

        [Theory]
        [MemberData(nameof(CentEdgeCases))]
        public void LucroBruto_ReceitaMenosCusto_ExatoEmCentavos(decimal receita, decimal custo, decimal lucroEsperado)
        {
            var receitaCents = MoneyCents.FromDecimal(receita);
            var custoCents = MoneyCents.FromDecimal(custo);
            var lucroCents = receitaCents - custoCents;

            Assert.Equal(MoneyCents.FromDecimal(lucroEsperado).Cents, lucroCents.Cents);
            Assert.Equal(0, lucroCents.Cents - MoneyCents.FromDecimal(lucroEsperado).Cents);

            // Espelha CriarLucroFinanceiroItem (receita - custo) em decimal
            var lucroDecimal = receita - custo;
            Assert.Equal(lucroEsperado, lucroDecimal);
            Assert.Equal(MoneyCents.FromDecimal(lucroEsperado).Cents, MoneyCents.FromDecimal(lucroDecimal).Cents);
        }

        [Fact]
        public void ObterLucroPorServico_IncluiServicoSinteticoComLucroExato_ELimite50()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "PrimoAutoEletrica_Test", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var logger = new LoggerService();
                var db = new DatabaseService(tempDir, logger: logger);
                var repo = new VendaRepository(db);

                var token = "C114_SMOKE_LUCRO_SERV_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var hoje = DateTime.Today;
                const decimal receita = 100.00m;
                const decimal custo = 40.00m;
                const decimal lucroEsperado = 60.00m;

                // Ruido: 60 servicos com lucro menor para provar que limit=50 ainda pega o nosso (maior lucro)
                for (var i = 0; i < 60; i++)
                {
                    InserirServico(repo, db, hoje, $"ruido_servico_{i:D3}", receita: 10m + (i * 0.01m), custo: 5m);
                }

                InserirServico(repo, db, hoje, token, receita, custo);

                var financeiro = new FinanceiroDatabaseService(db);
                var itens = financeiro.ObterLucroPorServico(hoje.AddDays(-1), hoje.AddDays(1), 50);

                Assert.True(itens.Count <= 50, $"limit 50 violado: {itens.Count}");
                var alvo = Assert.Single(itens.Where(i =>
                    i.Referencia != null &&
                    i.Referencia.Contains(token, StringComparison.OrdinalIgnoreCase)));

                Assert.Equal(receita, alvo.Receita);
                Assert.Equal(custo, alvo.Custo);
                Assert.Equal(lucroEsperado, alvo.LucroBruto);
                Assert.Equal(MoneyCents.FromDecimal(lucroEsperado).Cents, MoneyCents.FromDecimal(alvo.LucroBruto).Cents);
                Assert.Contains(token, alvo.Referencia, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { /* temp */ }
            }
        }

        private static void InserirServico(
            VendaRepository repo,
            DatabaseService db,
            DateTime data,
            string descricao,
            decimal receita,
            decimal custo)
        {
            var venda = new Venda
            {
                Id = Guid.NewGuid(),
                Data = data,
                Cliente = null,
                Itens = new List<ItemVenda>
                {
                    new ItemVenda
                    {
                        ProdutoId = null,
                        Quantidade = 1,
                        PrecoUnitario = receita,
                        CustoUnitario = custo,
                        Descricao = descricao,
                        Tipo = "Servico",
                        Desconto = 0m
                    }
                },
                Total = receita,
                FormaPagamento = "Dinheiro",
                Desconto = 0m,
                Usuario = "C1.1.4",
                Status = "Concluida"
            };

            using var conn = db.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            repo.InserirVenda(conn, tx, venda);
            foreach (var item in venda.Itens)
            {
                repo.InserirItemVenda(conn, tx, venda.Id, item);
            }
            tx.Commit();
        }
    }
}