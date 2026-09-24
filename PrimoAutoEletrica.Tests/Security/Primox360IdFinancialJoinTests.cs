using System;
using System.IO;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    /// <summary>
    /// Phase 1: Primox360 / Cliente360 / Veiculo360 nao usam TEXT_MATCH por nome em financeiro.
    /// </summary>
    public class Primox360IdFinancialJoinTests
    {
        [Fact]
        public void Primox360Service_NaoDeveUsarContainsDeNomeParaFinanceiro()
        {
            var src = File.ReadAllText(Locate("Services", "Primox360Service.cs"));

            Assert.Contains("ObterContasReceberVinculadasAoCliente", src);
            Assert.Contains("matchClienteId", src);
            Assert.Contains("OrigemOsContaReceber", src);
            Assert.Contains("OrigemOrcamentoContaReceber", src);
            Assert.Contains("TEXT_MATCH por nome", src);

            Assert.DoesNotContain("ClienteNome.Contains", src);
            Assert.DoesNotContain("NomeSnapshot.Contains", src);
            Assert.DoesNotContain("GetDyn(conta, \"Cliente\")).Contains", src);
            Assert.DoesNotContain("GetDyn(c, \"Cliente\")).Contains", src);
        }

        [Fact]
        public void Financeiro_ResolverClienteIdPorNome_DeveRecusarHomologos()
        {
            var src = File.ReadAllText(Locate("Services", "FinanceiroDatabaseService.cs"));
            Assert.Contains("ResolverClienteIdPorNome", src);
            Assert.Contains("matches.Count == 1", src);
            Assert.Contains("Homônimo ou zero", src);
            Assert.DoesNotContain("nomeCliente.Contains", src);
        }

        private static string Locate(string folder, string fileName)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "PrimoAutoEletrica", folder, fileName);
                if (File.Exists(candidate)) return candidate;
                candidate = Path.Combine(dir.FullName, folder, fileName);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException($"Nao encontrou {folder}/{fileName}");
        }
    }
}
