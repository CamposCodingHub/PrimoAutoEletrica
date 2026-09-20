using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    public class ApiAuthenticationGateTests
    {
        [Fact]
        public void Program_Cs_TemJwtUseAuthenticationERequireAuthorization()
        {
            var src = File.ReadAllText(LocateProgram());
            Assert.Contains("AddAuthentication", src);
            Assert.Contains("AddJwtBearer", src);
            Assert.Contains("UseAuthentication()", src);
            Assert.Contains("RequireAuthorization", src);
            Assert.Contains("ORCAMENTO_LER", src);
            Assert.Contains("ESTOQUE_LER", src);
            Assert.Contains("FINANCEIRO_LER", src);
            Assert.Contains("AllowAnonymous", src);
            Assert.Contains("/api/health", src);
            Assert.Contains("/api/auth/token", src);
        }

        [Fact]
        public void Program_Cs_NaoVazaExMessageEmProblem()
        {
            var src = File.ReadAllText(LocateProgram());
            Assert.Contains("SafeProblem", src);
            Assert.False(Regex.IsMatch(src, @"Results\.Problem\([^)]*ex\.Message"),
                "Handlers nao devem interpolar ex.Message em Results.Problem");
        }

        [Fact]
        public void Program_Cs_EndpointsNegocioProtegidos()
        {
            var src = File.ReadAllText(LocateProgram());
            Assert.Contains("MapGet(\"/api/orcamentos\"", src);
            Assert.Contains("MapPost(\"/api/financeiro/orcamento\"", src);
            Assert.Contains("MapGet(\"/api/estoque/produtos\"", src);
            Assert.Contains("RequireAuthorization(\"ORCAMENTO_LER\")", src);
            Assert.Contains("RequireAuthorization(\"FINANCEIRO_EDITAR\")", src);
            Assert.Contains("RequireAuthorization(\"ESTOQUE_LER\")", src);
        }


        [Fact]
        public void Program_Cs_RecusaPlaceholderSigningKeyForaDeDevelopment()
        {
            var src = File.ReadAllText(LocateProgram());
            Assert.Contains("CHANGE_ME", src);
            Assert.Contains("nao permitido fora de Development", src);
        }
        private static string LocateProgram()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "PrimoAutoEletrica.Api", "Program.cs");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException("PrimoAutoEletrica.Api/Program.cs nao encontrado");
        }
    }
}
