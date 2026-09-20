using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    /// <summary>
    /// P0 — testes HTTP reais contra a API (nao dicionario fake).
    /// Cobertura: ausente / invalido / expirado / sem perm / com perm / health anonimo.
    /// </summary>
    public class ApiJwtHttpTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private const string DevKey = "DEV_ONLY_PRIMOX_JWT_SIGNING_KEY_MIN_32_CHARS!!";
        private readonly WebApplicationFactory<Program> _factory;

        public ApiJwtHttpTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(b =>
            {
                b.UseEnvironment("Development");
                b.UseSetting("Jwt:SigningKey", DevKey);
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
        public async Task TokenDev_ComPermissao_Orcamentos_Nao401Nem403()
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
        public async Task TokenExpirado_Retorna401()
        {
            var token = MintToken(
                permissions: "ORCAMENTO_LER",
                notBefore: DateTime.UtcNow.AddHours(-2),
                expires: DateTime.UtcNow.AddHours(-1));

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.GetAsync("/api/orcamentos");
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        [Fact]
        public async Task TokenSemPermissaoOrcamento_Retorna403()
        {
            var token = MintToken(permissions: "ESTOQUE_LER");

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.GetAsync("/api/orcamentos");
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        }

        [Fact]
        public async Task TokenComPermissaoOrcamento_Nao401Nem403()
        {
            var token = MintToken(permissions: "ORCAMENTO_LER");

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.GetAsync("/api/orcamentos");
            Assert.NotEqual(HttpStatusCode.Unauthorized, res.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, res.StatusCode);
        }

        [Fact]
        public async Task SafeProblem_NaoVazaMensagemInterna_QuandoEndpointFalha()
        {
            // Forca rota financeira com token valido; corpo 500 (se houver) nao deve conter stack/SQL tipico
            var token = MintToken(permissions: "FINANCEIRO_LER");
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await client.GetAsync("/api/financeiro/resumo/2026-01-01/2026-01-31");
            var body = await res.Content.ReadAsStringAsync();

            Assert.DoesNotContain("at System.", body, StringComparison.Ordinal);
            Assert.DoesNotContain("SQLiteException", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Microsoft.Data.Sqlite", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
            // Se 500, deve ser mensagem publica SafeProblem
            if (res.StatusCode == HttpStatusCode.InternalServerError)
            {
                Assert.Contains("Erro ao obter resumo financeiro", body, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string MintToken(string permissions, DateTime? notBefore = null, DateTime? expires = null)
        {
            var claims = new[]
            {
                new Claim("sub", "test-client"),
                new Claim("perm", permissions)
            };
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevKey)),
                SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: "Primox.Api",
                audience: "Primox.Clients",
                claims: claims,
                notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-1),
                expires: expires ?? DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
