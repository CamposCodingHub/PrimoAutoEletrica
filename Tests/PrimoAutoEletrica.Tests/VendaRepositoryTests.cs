using System;
using System.IO;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class VendaRepositoryTests
    {
        [Fact]
        public void InserirEObterVenda_BasicFlow_Works()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "PrimoAutoEletrica_Test", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            var logger = new LoggerService();
            var db = new DatabaseService(tempDir, logger: logger);
            var repo = new VendaRepository(db);

            var venda = new Venda
            {
                Id = Guid.NewGuid(),
                Data = DateTime.Now,
                Cliente = null,
                Itens = new System.Collections.Generic.List<ItemVenda>
                {
                    new ItemVenda { ProdutoId = null, Quantidade = 1, PrecoUnitario = 10m, Descricao = "ItemTeste", Tipo = "Produto" }
                },
                Total = 10m,
                FormaPagamento = "Dinheiro",
                Desconto = 0m,
                Usuario = "Teste",
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

            using var conn2 = db.GetConnection();
            conn2.Open();
            var fetched = repo.ObterVendaInterna(conn2, null, venda.Id);
            Assert.NotNull(fetched);
            Assert.Equal(venda.Id, fetched!.Id);
            Assert.Single(fetched.Itens);
        }
    }
}
