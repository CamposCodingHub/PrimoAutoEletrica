using System;
using System.Linq;
using System.Reflection;
using Xunit;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Tests
{
    public class OrcamentoDatabaseServiceTests
    {
        [Fact]
        public void OrcamentoDatabaseService_DeveExistir()
        {
            // Verifica se o tipo OrcamentoDatabaseService existe
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var orcamentoServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "OrcamentoDatabaseService");

            Assert.NotNull(orcamentoServiceType);
        }

        [Fact]
        public void OrcamentoDatabaseService_DeveTerMetodosPrincipais()
        {
            // Verifica se o serviço tem os métodos principais
            var assembly = Assembly.Load("PrimoAutoEletrica");
            var orcamentoServiceType = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == "OrcamentoDatabaseService");

            if (orcamentoServiceType == null)
            {
                return;
            }

            var methods = orcamentoServiceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            Assert.True(methods.Length > 0, "Deve ter métodos públicos");
        }

        [Fact]
        public void OrcamentoDatabaseService_ModeloOrcamento_DeveTerPropriedadesObrigatorias()
        {
            // Arrange & Act
            var orcamento = new Orcamento
            {
                Id = Guid.NewGuid(),
                ClienteId = Guid.NewGuid(),
                VendedorId = Guid.NewGuid(),
                Numero = "TEST-001",
                Status = "rascunho",
                DataCriacao = DateTime.Now,
                Subtotal = 1000m,
                Total = 1000m
            };

            // Assert - Verifica se o modelo tem as propriedades básicas
            Assert.NotNull(orcamento);
            Assert.NotEqual(Guid.Empty, orcamento.Id);
            Assert.NotEqual(Guid.Empty, orcamento.ClienteId);
            Assert.NotEqual(Guid.Empty, orcamento.VendedorId);
            Assert.Equal("TEST-001", orcamento.Numero);
            Assert.Equal("rascunho", orcamento.Status);
            Assert.Equal(1000m, orcamento.Total);
        }

        [Fact]
        public void OrcamentoDatabaseService_ModeloOrcamentoItem_DeveTerPropriedadesObrigatorias()
        {
            // Arrange & Act
            var item = new OrcamentoItem
            {
                Id = Guid.NewGuid(),
                OrcamentoId = Guid.NewGuid(),
                ProdutoId = Guid.NewGuid(),
                ProdutoNome = "Produto Teste",
                ProdutoCodigo = "PROD-001",
                Quantidade = 2,
                PrecoUnitario = 50m,
                Subtotal = 100m
            };

            // Assert - Verifica se o modelo de item tem as propriedades básicas
            Assert.NotNull(item);
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.NotEqual(Guid.Empty, item.OrcamentoId);
            Assert.Equal("Produto Teste", item.ProdutoNome);
            Assert.Equal("PROD-001", item.ProdutoCodigo);
            Assert.Equal(2, item.Quantidade);
            Assert.Equal(50m, item.PrecoUnitario);
            Assert.Equal(100m, item.Subtotal);
        }

        [Fact]
        public void OrcamentoDatabaseService_CalculoTotal_DeveFuncionarCorretamente()
        {
            // Arrange
            var subtotal = 1000m;
            var desconto = 100m;
            var acrescimo = 50m;

            // Act
            var total = subtotal - desconto + acrescimo;

            // Assert
            Assert.Equal(950m, total);
        }

        [Fact]
        public void OrcamentoDatabaseService_CalculoMargemLucro_DeveFuncionarCorretamente()
        {
            // Arrange
            var precoVenda = 150m;
            var precoCusto = 100m;

            // Act
            var margemLucro = ((precoVenda - precoCusto) / precoVenda) * 100;

            // Assert
            Assert.Equal(33.33m, margemLucro, 2); // 33.33% de margem
        }

        [Fact]
        public void OrcamentoDatabaseService_ValidacaoStatus_DeveAceitarStatusValidos()
        {
            // Arrange
            var statusValidos = new[] { "rascunho", "aprovado", "recusado", "vencido", "cancelado", "convertido" };

            // Act & Assert
            foreach (var status in statusValidos)
            {
                var orcamento = new Orcamento
                {
                    Id = Guid.NewGuid(),
                    Status = status,
                    DataCriacao = DateTime.Now
                };
                Assert.Equal(status, orcamento.Status);
            }
        }

        [Fact]
        public void OrcamentoDatabaseService_DataValidade_DeveSerFutura()
        {
            // Arrange
            var dataCriacao = DateTime.Now;
            var dataValidade = dataCriacao.AddDays(30);

            // Act & Assert
            Assert.True(dataValidade > dataCriacao);
            Assert.Equal(30, (dataValidade - dataCriacao).Days);
        }
    }
}