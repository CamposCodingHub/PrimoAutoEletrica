using System;
using System.IO;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Services
{
    public class OrcamentoOperacionalNullMoneyTests
    {
        [Fact]
        public void ObterTodosOrcamentos_FromOperacionalCopy_WithNullAcrescimo_DoesNotThrow()
        {
            var source = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
                "primoauto_operacional.db");
            Assert.True(File.Exists(source), "primoauto_operacional.db must exist for C1.1.2 BUG-004 proof");

            var tempRoot = Path.Combine(Path.GetTempPath(), "primox-c112-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            try
            {
                var dest = Path.Combine(tempRoot, "primoauto_operacional.db");
                File.Copy(source, dest, overwrite: true);

                var settings = new DatabaseConnectionSettings
                {
                    Provider = "SQLite",
                    SQLitePath = "primoauto_operacional.db"
                };

                var db = new DatabaseService(tempRoot, settings, new LoggerService());
                var svc = new OrcamentoDatabaseService(db);
                var list = svc.ObterTodosOrcamentos();
                Assert.True(list.Count >= 1, "expected orcamentos in operacional copy");
                Assert.All(list, o =>
                {
                    Assert.True(o.Acrescimo >= 0m);
                    Assert.True(o.Desconto >= 0m);
                    Assert.True(o.Total >= 0m);
                });
            }
            finally
            {
                try { Directory.Delete(tempRoot, recursive: true); } catch { }
            }
        }
    }
}