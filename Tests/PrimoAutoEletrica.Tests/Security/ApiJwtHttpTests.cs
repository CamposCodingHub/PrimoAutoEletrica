using System;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    /// <summary>
    /// P0.02 — testes HTTP reais contra a API (nao dicionario fake).
    /// </summary>
    public class ApiJwtHttpTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ApiJwtHttpTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(b =>
            {
                b.UseEnvironment("Development");
                b.UseSetting("Jwt:SigningKey", "DEV_ONLY_PRIMOX_JWT_SIGNING_KEY_MIN_32_CHARS!!");
            });
        }

        [Fact]
        public async Task Health_SemToken_Retorna200()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/api/health");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        [Fact]
        public async Task Orcamentos_SemToken_Retorna401()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/api/orcamentos");
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Estoque_SemToken_Retorna401()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/api/estoque/produtos");
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task Financeiro_SemToken_Retorna401()
        {
            var client = _factory.CreateClient();
            var res = await client.GetAsync("/api/financeiro/resumo/2026-01-01/2026-01-31");
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task TokenDev_ComPermissao_Orcamentos_Nao401()
        {
            var client = _factory.CreateClient();
            var tokenRes = await client.PostAsJsonAsync("/api/auth/token", new { clientId = "dev", clientSecret = "dev" });
            Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
            using var doc = JsonDocument.Parse(await tokenRes.Content.ReadAsStringAsync());
            var jwt = doc.RootElement.GetProperty("access_token").GetString();
            Assert.False(string.IsNullOrWhiteSpace(jwt));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            var res = await client.GetAsync("/api/orcamentos");
            // Autorizado: 200 ou 500 de DB de teste — nunca 401/403
            Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
        }

        [Fact]
        public async Task TokenInvalido_Retorna401()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "nao.e.um.jwt.valido");
            var res = await client.GetAsync("/api/orcamentos");
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task TokenSemPermissaoOrcamento_Retorna403()
        {
            // Token so com ESTOQUE_LER — orcamentos exige ORCAMENTO_LER
            var key = "DEV_ONLY_PRIMOX_JWT_SIGNING_KEY_MIN_32_CHARS!!";
            var claims = new[]
            {
                new System.Security.Claims.Claim("sub", "limited"),
                new System.Security.Claims.Claim("perm", "ESTOQUE_LER")
            };
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
            var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
                issuer: "Primox.Api",
                audience: "Primox.Clients",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);
            var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(jwt);

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.GetAsync("/api/orcamentos");
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }
    }
}
