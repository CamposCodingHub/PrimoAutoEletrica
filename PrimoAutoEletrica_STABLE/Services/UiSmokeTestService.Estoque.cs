using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        // Checks operacionais de estoque.

        private void RunEstoqueOperationalChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Estoque:EntradaSaidaHistoricoPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("entrada e saida de estoque pela tela");
                var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do estoque nao foi preparada.");
                var produtoAntes = App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)
                    ?? throw new InvalidOperationException("Produto sintetico nao encontrado antes da movimentacao de estoque.");
                var quantidadeInicial = produtoAntes.QuantidadeEstoque;
                var hostWindow = CreateHostWindow(new EstoqueControl(), nameof(EstoqueControl));

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not EstoqueControl control)
                    {
                        throw new InvalidOperationException("Host de EstoqueControl nao conseguiu carregar entradas e saidas.");
                    }

                    var dataGrid = FindElementByName<DataGrid>(control, "ProdutosDataGrid")
                        ?? throw new InvalidOperationException("ProdutosDataGrid nao foi localizado para validar entradas e saidas.");

                    WaitForCondition(
                        () => LocalizarProdutoNoEstoque(dataGrid, fixture.Produto.Id) != null,
                        TimeSpan.FromSeconds(5),
                        "O produto sintetico nao apareceu na grade de estoque.");

                    SelecionarProdutoNoEstoque(dataGrid, fixture.Produto.Id);
                    ClickButton(control, "EntradaEstoqueButton");
                    WaitForCondition(
                        () => App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)?.QuantidadeEstoque == quantidadeInicial + 1,
                        TimeSpan.FromSeconds(5),
                        "A entrada dedicada acionada pela tela nao incrementou o estoque.");

                    SelecionarProdutoNoEstoque(dataGrid, fixture.Produto.Id);
                    ClickButton(control, "SaidaEstoqueButton");
                    WaitForCondition(
                        () => App.Repositories.Produtos.ObterPorId(fixture.Produto.Id)?.QuantidadeEstoque == quantidadeInicial,
                        TimeSpan.FromSeconds(5),
                        "A saida dedicada acionada pela tela nao restaurou o estoque esperado.");

                    SelecionarProdutoNoEstoque(dataGrid, fixture.Produto.Id);
                    var item = LocalizarProdutoNoEstoque(dataGrid, fixture.Produto.Id)
                        ?? throw new InvalidOperationException("Produto movimentado desapareceu da grade de estoque.");
                    var quantidadeEstoque = LerPropriedadeInteira(item, "QuantidadeEstoque");
                    var quantidadeReservada = LerPropriedadeInteira(item, "QuantidadeReservada");
                    var quantidadeDisponivel = LerPropriedadeInteira(item, "QuantidadeDisponivel");

                    if (quantidadeEstoque != quantidadeInicial ||
                        quantidadeDisponivel != quantidadeEstoque - quantidadeReservada)
                    {
                        throw new InvalidOperationException(
                            $"Saldo operacional inconsistente apos entrada/saida. Estoque={quantidadeEstoque}; Reservado={quantidadeReservada}; Disponivel={quantidadeDisponivel}.");
                    }

                    var historico = new EstoqueOperationalService(App.Database, _logger)
                        .ObterHistoricoProduto(fixture.Produto.Id, limite: 20);
                    if (!historico.Any(itemHistorico => string.Equals(itemHistorico.Acao, "EntradaEstoqueDedicada", StringComparison.OrdinalIgnoreCase)) ||
                        !historico.Any(itemHistorico => string.Equals(itemHistorico.Acao, "SaidaEstoqueDedicada", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException("O historico operacional nao registrou a entrada e a saida dedicadas.");
                    }
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });

            RunCheck(result, "Estoque:FiltrosOperacionaisPelaTela", () =>
            {
                GarantirBancoIsoladoDoSmoke("filtros operacionais do estoque");
                var produtoBaixo = CreatePersistedProdutoEstoqueSmoke(
                    "Baixo",
                    quantidadeEstoque: 1,
                    quantidadeMinima: 2,
                    quantidadeMaxima: 10,
                    precoCompra: 5m,
                    semCodigoOperacional: true,
                    dataUltimaVenda: DateTime.Today.AddDays(-120),
                    totalVendas: 7,
                    vendasUltimoMes: 0);
                var produtoAlto = CreatePersistedProdutoEstoqueSmoke(
                    "Alto",
                    quantidadeEstoque: 50,
                    quantidadeMinima: 2,
                    quantidadeMaxima: 10,
                    precoCompra: 100m,
                    semCodigoOperacional: false,
                    dataUltimaVenda: DateTime.Today,
                    totalVendas: 20,
                    vendasUltimoMes: 4);
                var produtoSemPreco = CreatePersistedProdutoEstoqueSmoke(
                    "SemPreco",
                    quantidadeEstoque: 8,
                    quantidadeMinima: 2,
                    quantidadeMaxima: 20,
                    precoCompra: 12m,
                    semCodigoOperacional: false,
                    dataUltimaVenda: DateTime.Today.AddDays(-10),
                    totalVendas: 1,
                    vendasUltimoMes: 0);
                produtoSemPreco.PrecoVenda = 0m;
                produtoSemPreco.MargemLucro = 0m;
                App.Repositories.Produtos.Atualizar(produtoSemPreco);

                var produtoSemFornecedor = CreatePersistedProdutoEstoqueSmoke(
                    "SemFornecedor",
                    quantidadeEstoque: 9,
                    quantidadeMinima: 2,
                    quantidadeMaxima: 20,
                    precoCompra: 14m,
                    semCodigoOperacional: false,
                    dataUltimaVenda: DateTime.Today.AddDays(-8),
                    totalVendas: 2,
                    vendasUltimoMes: 0);
                produtoSemFornecedor.Fornecedor = string.Empty;
                produtoSemFornecedor.FornecedorId = null;
                produtoSemFornecedor.CNPJFornecedor = string.Empty;
                App.Repositories.Produtos.Atualizar(produtoSemFornecedor);

                var produtoMargemBaixa = CreatePersistedProdutoEstoqueSmoke(
                    "MargemBaixa",
                    quantidadeEstoque: 10,
                    quantidadeMinima: 2,
                    quantidadeMaxima: 20,
                    precoCompra: 10m,
                    semCodigoOperacional: false,
                    dataUltimaVenda: DateTime.Today.AddDays(-6),
                    totalVendas: 3,
                    vendasUltimoMes: 0);
                produtoMargemBaixa.PrecoVenda = 11m;
                produtoMargemBaixa.MargemLucro = Math.Round(((produtoMargemBaixa.PrecoVenda - produtoMargemBaixa.PrecoCompra) / produtoMargemBaixa.PrecoVenda) * 100m, 2);
                App.Repositories.Produtos.Atualizar(produtoMargemBaixa);
                var hostWindow = CreateHostWindow(new EstoqueControl(), nameof(EstoqueControl));

                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not EstoqueControl control)
                    {
                        throw new InvalidOperationException("Host de EstoqueControl nao conseguiu carregar os filtros.");
                    }

                    var dataGrid = FindElementByName<DataGrid>(control, "ProdutosDataGrid")
                        ?? throw new InvalidOperationException("ProdutosDataGrid nao foi localizado para validar filtros.");
                    var statusFiltro = FindElementByName<ComboBox>(control, "StatusFiltroComboBox")
                        ?? throw new InvalidOperationException("StatusFiltroComboBox nao foi localizado para validar filtros.");

                    ValidarFiltroEstoque(statusFiltro, dataGrid, "Estoque Baixo", produtoBaixo.Nome, new[] { produtoAlto.Nome });
                    ValidarFiltroEstoque(statusFiltro, dataGrid, "Estoque Alto", produtoAlto.Nome, new[] { produtoBaixo.Nome });
                    ValidarFiltroEstoque(statusFiltro, dataGrid, "Produtos Parados", produtoBaixo.Nome, new[] { produtoAlto.Nome });
                    ValidarFiltroEstoque(statusFiltro, dataGrid, "Sem Codigo/SKU", produtoBaixo.Nome, new[] { produtoAlto.Nome });
                    ValidarFiltroEstoque(statusFiltro, dataGrid, "Sem Preco", produtoSemPreco.Nome, new[] { produtoAlto.Nome });
                    ValidarFiltroEstoque(statusFiltro, dataGrid, "Sem Fornecedor", produtoSemFornecedor.Nome, new[] { produtoAlto.Nome });
                    ValidarFiltroEstoque(statusFiltro, dataGrid, "Margem Baixa", produtoMargemBaixa.Nome, new[] { produtoAlto.Nome });
                    ValidarFiltroEstoque(statusFiltro, dataGrid, "Vendidos no Mes", produtoAlto.Nome, new[] { produtoBaixo.Nome });
                    ValidarFiltroEstoque(statusFiltro, dataGrid, "Curva A", produtoAlto.Nome, Array.Empty<string>());

                    statusFiltro.SelectedItem = "Mais Vendidos";
                    WaitForUiIdle();
                    var ranking = ObterNomesProdutosEstoque(dataGrid);
                    var indiceAlto = ranking.IndexOf(produtoAlto.Nome);
                    var indiceBaixo = ranking.IndexOf(produtoBaixo.Nome);
                    if (indiceAlto < 0 || indiceBaixo < 0 || indiceAlto >= indiceBaixo)
                    {
                        throw new InvalidOperationException("O ranking Mais Vendidos nao ordenou os produtos sinteticos pela quantidade vendida.");
                    }

                    foreach (var resumo in new[] { "MaisVendidosResumoText", "CurvaAbcResumoText", "BaixoGiroResumoText" })
                    {
                        var texto = FindElementByName<TextBlock>(control, resumo)?.Text;
                        if (string.IsNullOrWhiteSpace(texto))
                        {
                            throw new InvalidOperationException($"O resumo operacional '{resumo}' ficou vazio.");
                        }
                    }
                }
                finally
                {
                    if (hostWindow.IsVisible)
                    {
                        hostWindow.Close();
                    }
                }
            });
        }

    }
}
