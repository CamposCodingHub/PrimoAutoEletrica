using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    /// <summary>
    /// P0 — Production nao sobe com SigningKey placeholder.
    /// </summary>
    public class ApiJwtProductionBootstrapTests
    {
        [Fact]
        public void Production_ComChangeMe_NaoSobeHost()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            {
                b.UseEnvironment(Environments.Production);
                b.UseSetting("Jwt:SigningKey", "CHANGE_ME_PRIMOX_JWT_SIGNING_KEY_MIN_32");
            });

            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                using var client = factory.CreateClient();
                _ = client.GetAsync("/api/health").GetAwaiter().GetResult();
            });

            Assert.Contains("CHANGE_ME", Flatten(ex), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Production_ComDevOnlyPrefix_NaoSobeHost()
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            {
                b.UseEnvironment(Environments.Production);
                b.UseSetting("Jwt:SigningKey", "DEV_ONLY_PRIMOX_JWT_SIGNING_KEY_MIN_32_CHARS!!");
            });

            var ex = Assert.ThrowsAny<Exception>(() =>
            {
                using var client = factory.CreateClient();
                _ = client.GetAsync("/api/health").GetAwaiter().GetResult();
            });

            Assert.Contains("DEV_ONLY", Flatten(ex), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Production_ComChaveForte_HealthAnonimo200()
        {
            const string strong = "PROD_REAL_PRIMOX_JWT_KEY_AT_LEAST_32_CHARS_OK!!";
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            {
                b.UseEnvironment(Environments.Production);
                b.UseSetting("Jwt:SigningKey", strong);
                b.UseSetting("Jwt:Clients:0:ClientId", "primox-desktop");
                b.UseSetting("Jwt:Clients:0:ClientSecret", "prod-desktop-secret-not-change-me");
                b.UseSetting("Jwt:Clients:0:Permissions", "ORCAMENTO_LER");
            });

            using var client = factory.CreateClient();
            var res = client.GetAsync("/api/health").GetAwaiter().GetResult();
            Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);

            var orc = client.GetAsync("/api/orcamentos").GetAwaiter().GetResult();
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, orc.StatusCode);
        }

        private static string Flatten(Exception ex)
        {
            var s = ex.ToString();
            if (ex.InnerException != null) s += " | " + ex.InnerException;
            return s;
        }
    }
}
