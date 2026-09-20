using System;
using System.IO;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    /// <summary>
    /// P0-01 — gate honesto sobre autenticação da API.
    /// </summary>
    public class ApiAuthenticationGateTests
    {
        [Fact]
        public void Program_Cs_EstadoAtual_SemJwt_DocumentadoComoRiscoCritico()
        {
            var src = File.ReadAllText(LocateProgram());

            // Evidência CODE: sem schema JWT / sem RequireAuthorization em endpoints de negócio.
            Assert.DoesNotContain("AddJwtBearer", src);
            Assert.DoesNotContain("AddAuthentication", src);
            Assert.DoesNotContain("RequireAuthorization", src);

            Assert.Contains("MapGet(\"/api/orcamentos\"", src);
            Assert.Contains("MapPost(\"/api/financeiro/orcamento\"", src);
            Assert.Contains("MapGet(\"/api/estoque/produtos\"", src);

            // UseAuthorization sem AddAuthentication = não protege endpoints minimal APIs.
            Assert.Contains("UseAuthorization()", src);

            // Results.Problem vaza ex.Message em vários handlers.
            Assert.Contains("Results.Problem", src);
            Assert.Contains("ex.Message", src);
        }

        [Fact(Skip = "P0-01 GATE: remover Skip somente após ligar JWT + RequireAuthorization nos endpoints de negócio (não no /api/health).")]
        public void Program_Cs_DeveTerJwtERequireAuthorization_AntesDeProducao()
        {
            var src = File.ReadAllText(LocateProgram());
            Assert.Contains("AddAuthentication", src);
            Assert.Contains("AddJwtBearer", src);
            Assert.Contains("RequireAuthorization", src);
            Assert.Contains("UseAuthentication()", src);
        }

        private static string LocateProgram()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "PrimoAutoEletrica.Api", "Program.cs");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new FileNotFoundException("PrimoAutoEletrica.Api/Program.cs não encontrado");
        }
    }
}
