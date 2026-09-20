using System;
using System.IO;
using System.Linq;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    /// <summary>
    /// Regressão P0-03: histórico financeiro do cliente não pode casar por nome parcial.
    /// </summary>
    public class HistoricoClienteFinancialIsolationTests
    {
        [Fact]
        public void HistoricoClienteWindow_NaoDeveConterMatchingFinanceiroPorNome()
        {
            var path = Locate("HistoricoClienteWindow.xaml.cs");
            var src = File.ReadAllText(path);

            Assert.DoesNotContain("ClienteCorresponde", src);
            Assert.Contains("ObterContasReceberVinculadasAoCliente", src);
            Assert.Contains("_cliente.Id", src);
            Assert.Contains("P0-03", src);

            Assert.DoesNotContain("clienteAtual.Contains(clienteFinanceiro", src);
            Assert.DoesNotContain("clienteFinanceiro.Contains(clienteAtual", src);
        }

        [Fact]
        public void DoisClientesComNomesSimilares_NaoCompartilhamContas_PorId()
        {
            var joaoSilva = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var joaoSilvaJunior = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            var contas = new[]
            {
                new { Id = 1, ClienteId = joaoSilva, NomeSnapshot = "JOAO SILVA", Valor = 100m },
                new { Id = 2, ClienteId = joaoSilvaJunior, NomeSnapshot = "JOAO SILVA JUNIOR", Valor = 50m },
                new { Id = 3, ClienteId = joaoSilva, NomeSnapshot = "JOAO SILVA", Valor = 25m },
            };

            var doJoao = contas.Where(c => c.ClienteId == joaoSilva).Select(c => c.Id).ToArray();
            var doJunior = contas.Where(c => c.ClienteId == joaoSilvaJunior).Select(c => c.Id).ToArray();

            Assert.Equal(new[] { 1, 3 }, doJoao);
            Assert.Equal(new[] { 2 }, doJunior);
            Assert.Empty(doJoao.Intersect(doJunior));

            var antiPadraoPorNome = contas
                .Where(c => c.NomeSnapshot.Contains("JOAO SILVA", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Id)
                .ToArray();
            Assert.Equal(3, antiPadraoPorNome.Length);
            Assert.NotEqual(doJoao.OrderBy(x => x).ToArray(), antiPadraoPorNome.OrderBy(x => x).ToArray());
        }

        private static string Locate(string fileName)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "PrimoAutoEletrica", "Views", fileName);
                if (File.Exists(candidate)) return candidate;
                candidate = Path.Combine(dir.FullName, "Views", fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException($"Não encontrou {fileName} a partir de {AppContext.BaseDirectory}");
        }
    }
}
