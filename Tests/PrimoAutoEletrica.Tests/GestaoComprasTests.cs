using System;
using System.Collections.Generic;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class GestaoComprasTests
    {
        [Fact]
        public void ItemFaltaEstoque_CalculosBasicos_EstaoCorretos()
        {
            var item = new ItemFaltaEstoque
            {
                ProdutoId = Guid.NewGuid(),
                Codigo = "REL-001",
                Nome = "Relé Auxiliar 4 Pinos 12V 40A",
                QuantidadeEstoque = 5,
                QuantidadeReservadaOS = 3,
                QuantidadeSugeridaCompra = 10,
                UltimoCustoCompra = 12.50m,
                Urgencia = NivelUrgenciaFalta.Alta
            };

            Assert.Equal(2, item.SaldoRealDisponivel);
            Assert.Equal(125.00m, item.ValorTotalEstimado);
            Assert.Equal("Alta (Abaixo Mínimo)", item.UrgenciaDescricao);
        }

        [Fact]
        public void PedidoCompraItem_SubtotalEPendencias_EstaoCorretos()
        {
            var item = new PedidoCompraItem
            {
                Id = Guid.NewGuid(),
                Codigo = "BAT-60",
                Descricao = "Bateria Moura 60Ah M60GD",
                QuantidadePedida = 8,
                QuantidadeRecebida = 5,
                ValorUnitario = 380.00m
            };

            Assert.Equal(3040.00m, item.Subtotal);
            Assert.Equal(3, item.PendenteRecebimento);
        }

        [Fact]
        public void GestaoComprasService_FormatarMensagemWhatsApp_GeraTextoEstruturado()
        {
            var pedido = new PedidoCompra
            {
                Numero = "PC-2026-0042",
                FornecedorNome = "Distribuidora Elétrica Express",
                DataCriacao = new DateTime(2026, 10, 3, 14, 30, 0),
                Itens = new List<PedidoCompraItem>
                {
                    new()
                    {
                        Codigo = "FUS-MAXI-30",
                        Descricao = "Fusível Maxi Lâmina 30A Verde",
                        QuantidadePedida = 50,
                        ValorUnitario = 1.20m
                    },
                    new()
                    {
                        Codigo = "REG-IKRO-044",
                        Descricao = "Regulador de Voltagem Alternador Bosch",
                        QuantidadePedida = 4,
                        ValorUnitario = 85.00m
                    }
                }
            };

            var service = new GestaoComprasService(
                App.Database,
                App.Repositories.Produtos,
                App.Repositories.Fornecedores,
                new EstoqueOperationalService(App.Database, App.Logger));

            var mensagem = service.FormatarMensagemCotacaoWhatsApp(pedido, "PRIMOX Workshop");

            Assert.NotNull(mensagem);
            Assert.Contains("PC-2026-0042", mensagem);
            Assert.Contains("Distribuidora Elétrica Express", mensagem);
            Assert.Contains("FUS-MAXI-30", mensagem);
            Assert.Contains("REG-IKRO-044", mensagem);
            Assert.Contains("50 un", mensagem);
            Assert.Contains("4 un", mensagem);
        }

        [Fact]
        public void DocumentoPdfService_GerarPedidoCompra_CriaArquivoValido()
        {
            var pedido = new PedidoCompra
            {
                Numero = "PC-TEST-0001",
                FornecedorNome = "Autopeças Brasil LTDA",
                FornecedorCNPJ = "12.345.678/0001-90",
                FornecedorTelefone = "(11) 98765-4321",
                FornecedorEmail = "vendas@autopecasbrasil.com",
                Status = StatusPedidoCompra.AprovadoAguardandoEntrega,
                ValorTotal = 450.00m,
                DataCriacao = DateTime.Now,
                PrevisaoEntrega = DateTime.Now.AddDays(2),
                FormaPagamento = "Boleto 28D",
                CondicaoPagamento = "Frete CIF",
                Itens = new List<PedidoCompraItem>
                {
                    new()
                    {
                        Codigo = "LAMP-H4-12V",
                        Descricao = "Lâmpada H4 12V 60/55W Super Branca",
                        QuantidadePedida = 20,
                        ValorUnitario = 15.00m
                    },
                    new()
                    {
                        Codigo = "TERM-ILHO-10",
                        Descricao = "Terminal Ilhós Tubular 10mm² Isolado",
                        QuantidadePedida = 100,
                        ValorUnitario = 1.50m
                    }
                }
            };

            var pdfService = new DocumentoPdfService();
            var caminhoTemp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TestePedido_{Guid.NewGuid():N}.pdf");

            try
            {
                var arquivoGerado = pdfService.GerarPedidoCompra(pedido, caminhoTemp);
                Assert.True(System.IO.File.Exists(arquivoGerado));
                var tamanho = new System.IO.FileInfo(arquivoGerado).Length;
                Assert.True(tamanho > 1000, "PDF deve ter mais de 1KB gerado.");
            }
            finally
            {
                if (System.IO.File.Exists(caminhoTemp))
                {
                    try { System.IO.File.Delete(caminhoTemp); } catch { }
                }
            }
        }
    }
}
