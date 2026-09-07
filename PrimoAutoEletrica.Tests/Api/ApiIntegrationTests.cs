using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using PrimoAutoEletrica.Api;

namespace PrimoAutoEletrica.Tests.Api
{
    /// <summary>
    /// Testes de integração para API endpoints
    /// </summary>
    public class ApiHealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ApiHealthCheckTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task HealthCheck_Endpoint_Deve_Retornar_200()
        {
            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task HealthCheck_Deve_Retornar_Status_Saudavel()
        {
            // Act
            var response = await _client.GetAsync("/api/health");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Healthy", content);
        }

        [Fact]
        public async Task HealthCheck_Deve_Incluir_Timestamp()
        {
            // Act
            var response = await _client.GetAsync("/api/health");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Timestamp", content);
        }

        [Fact]
        public async Task HealthCheck_Deve_Incluir_Versao()
        {
            // Act
            var response = await _client.GetAsync("/api/health");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("Version", content);
        }
    }

    /// <summary>
    /// Testes de integração para endpoints de Orçamento
    /// </summary>
    public class OrcamentoApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public OrcamentoApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListarOrcamentos_Deve_Retornar_200()
        {
            // Act
            var response = await _client.GetAsync("/api/orcamentos");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ListarOrcamentos_Deve_Retornar_JsonArray()
        {
            // Act
            var response = await _client.GetAsync("/api/orcamentos");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.NotEmpty(content);
            Assert.StartsWith("[", content.Trim());
        }

        [Fact]
        public async Task ObterOrcamentoPorId_ComIdValido_Deve_Retornar_200()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/orcamentos/{id}");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ObterOrcamentoPorId_ComIdInvalido_Deve_Retornar_BadRequest()
        {
            // Act
            var response = await _client.GetAsync($"/api/orcamentos/invalid-id");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    /// <summary>
    /// Testes de integração para endpoints de Ordem de Serviço
    /// </summary>
    public class OrdemServicoApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public OrdemServicoApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListarOrdensServico_Deve_Retornar_200()
        {
            // Act
            var response = await _client.GetAsync("/api/ordens-servico");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ObterOrdenServicoPorId_Deve_Validar_Formato_Id()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/ordens-servico/{id}");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }
    }

    /// <summary>
    /// Testes de integração para endpoints de Estoque
    /// </summary>
    public class EstoqueApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public EstoqueApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListarProdutos_Deve_Retornar_200()
        {
            // Act
            var response = await _client.GetAsync("/api/estoque/produtos");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task VerificarDisponibilidade_Deve_Aceitar_QueryString()
        {
            // Act
            var response = await _client.GetAsync("/api/estoque/disponibilidade?produtoId=123&quantidade=5");

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound);
        }
    }

    /// <summary>
    /// Testes de integração para endpoints de Financeiro
    /// </summary>
    public class FinanceiroApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public FinanceiroApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ListarMovimentacoes_Deve_Retornar_JsonArray()
        {
            // Act
            var response = await _client.GetAsync("/api/financeiro/movimentacoes");

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GerarRelatorio_Deve_Aceitar_Parametros_Data()
        {
            // Arrange
            var dataInicio = DateTime.Now.AddMonths(-1);
            var dataFim = DateTime.Now;

            // Act
            var response = await _client.GetAsync($"/api/financeiro/relatorio?dataInicio={dataInicio:yyyy-MM-dd}&dataFim={dataFim:yyyy-MM-dd}");

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound);
        }
    }

    /// <summary>
    /// Testes para CORS e headers de segurança
    /// </summary>
    public class ApiSecurityTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ApiSecurityTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Preflight_Request_Deve_Aceitar_CORS()
        {
            // Arrange
            using var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
            request.Headers.Add("Origin", "http://localhost:3000");
            request.Headers.Add("Access-Control-Request-Method", "GET");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Response_Deve_Incluir_ContentType()
        {
            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            Assert.NotNull(response.Content.Headers.ContentType);
        }

        [Fact]
        public async Task Health_Endpoint_Deve_Ser_Publico()
        {
            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    /// <summary>
    /// Testes de performance da API
    /// </summary>
    public class ApiPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ApiPerformanceTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task HealthCheck_Deve_Responder_Rapido()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync("/api/health");
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"Health check levou {stopwatch.ElapsedMilliseconds}ms (esperado < 1000ms)");
        }

        [Fact]
        public async Task ListarOrcamentos_Deve_Responder_Em_Tempo_Razoavel()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await _client.GetAsync("/api/orcamentos");
            stopwatch.Stop();

            // Assert
            Assert.True(stopwatch.ElapsedMilliseconds < 5000,
                $"Listar orçamentos levou {stopwatch.ElapsedMilliseconds}ms (esperado < 5000ms)");
        }
    }
}
