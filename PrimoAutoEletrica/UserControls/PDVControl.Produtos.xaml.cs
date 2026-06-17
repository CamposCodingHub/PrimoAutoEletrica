using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Printing;
using System.IO;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrimoAutoEletrica.UserControls
{
    public partial class PDVControl
    {
        // Busca, selecao e inclusao de produtos no carrinho.

        private void BuscarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            RealizarBuscaProduto();
        }

        private void AbrirSelecionarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirJanelaSelecionarProduto();
        }

        private void AbrirJanelaSelecionarProduto()
        {
            try
            {
                if (_todosProdutos.Count == 0)
                {
                    CarregarDadosIniciais();
                }

                if (_todosProdutos.Count == 0)
                {
                    ExibirMensagem(
                        "Nenhum produto encontrado no estoque.\n\nCadastre ou importe produtos antes de usar a seleção do PDV.",
                        "Selecionar produto",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                var janela = new SelecionarProdutoPDVWindow(_todosProdutos);
                ConfigurarOwner(janela);

                if (janela.ShowDialog() == true && janela.ProdutoSelecionado != null)
                {
                    _viewModel.SelectedProduto = janela.ProdutoSelecionado;
                    AdicionarProdutoAoCarrinho(janela.ProdutoSelecionado);

                    if (!janela.FecharAposAdicionar)
                    {
                        Dispatcher.BeginInvoke(new Action(AbrirJanelaSelecionarProduto), DispatcherPriority.ContextIdle);
                    }
                }
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Erro ao abrir seleção de produtos:\n{ex.Message}",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BuscaProdutoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RealizarBuscaProduto();
        }

        private void BuscaProdutoTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RealizarBuscaProduto();
                e.Handled = true;
            }
        }

        private void RealizarBuscaProduto()
        {
            var termo = BuscaProdutoTextBox.Text.Trim().ToUpperInvariant();

            _viewModel.Produtos.Clear();

            IEnumerable<Produto> produtosFiltrados = _todosProdutos;

            if (!string.IsNullOrWhiteSpace(_categoriaSelecionada))
            {
                produtosFiltrados = produtosFiltrados
                    .Where(p => string.Equals(p.Categoria, _categoriaSelecionada, StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrWhiteSpace(termo))
            {
                AtualizarProdutosVisiveis(produtosFiltrados.Take(10));
                return;
            }

            var resultados = produtosFiltrados
                .Where(p =>
                    TextoContem(p.Nome, termo) ||
                    TextoContem(p.Codigo, termo) ||
                    TextoContem(p.SKU, termo) ||
                    TextoContem(p.CodigoBarras, termo) ||
                    TextoContem(p.Categoria, termo) ||
                    TextoContem(p.Marca, termo))
                .Take(30)
                .ToList();

            AtualizarProdutosVisiveis(resultados);

            if (resultados.Count == 1)
            {
                _viewModel.SelectedProduto = resultados[0];
                AdicionarProdutoAoCarrinho(resultados[0]);
            }
            else if (resultados.Count > 1)
            {
                AbrirJanelaSelecionarProduto();
            }
            else
            {
                ExibirMensagem(
                    "Nenhum produto encontrado para a busca informada.",
                    "Busca de produto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void CategoriaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string categoria)
            {
                _categoriaSelecionada = categoria;

                BuscaProdutoTextBox.Clear();

                IEnumerable<Produto> produtosFiltrados = _todosProdutos;

                if (!string.IsNullOrWhiteSpace(_categoriaSelecionada))
                {
                    produtosFiltrados = produtosFiltrados
                        .Where(p => string.Equals(p.Categoria, _categoriaSelecionada, StringComparison.OrdinalIgnoreCase));
                }

                AtualizarProdutosVisiveis(produtosFiltrados.Take(30));
            }
        }

        private void FormaPagamentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string formaPagamento)
            {
                return;
            }

            _viewModel.FormaPagamentoSelecionada = formaPagamento;
            _viewModel.PagamentoMistoResumo = string.Equals(formaPagamento, "Misto", StringComparison.OrdinalIgnoreCase)
                ? "Pagamento misto: rateio sera conferido ao finalizar."
                : string.Empty;

            AtualizarFormaPagamentoSelecionada();
        }

        private void AdicionarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: Produto produto })
            {
                _viewModel.SelectedProduto = produto;
                AdicionarProdutoAoCarrinho(produto);
            }
        }

        private void ProdutosListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedProduto != null)
            {
                AdicionarProdutoAoCarrinho(_viewModel.SelectedProduto);
            }
        }

        private void AdicionarProdutoAoCarrinho(Produto produto)
        {
            var disponibilidade = ObterDisponibilidadeOperacional(produto);

            if (disponibilidade <= 0)
            {
                ExibirMensagem(
                    "Produto sem disponibilidade operacional no momento.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var itemExistente = _viewModel.Carrinho
                .FirstOrDefault(i => i.Produto != null && i.Produto.Id == produto.Id);

            if (itemExistente != null)
            {
                if (itemExistente.Quantidade + 1 > disponibilidade)
                {
                    ExibirMensagem(
                        $"Disponibilidade operacional insuficiente. Restante: {Math.Max(0, disponibilidade - itemExistente.Quantidade)}.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                itemExistente.Quantidade++;
            }
            else
            {
                _viewModel.Carrinho.Add(new ItemVenda
                {
                    Produto = produto,
                    ProdutoId = produto.Id,
                    Tipo = "Produto",
                    Descricao = produto.Nome,
                    Quantidade = 1,
                    PrecoUnitario = produto.PrecoVenda,
                    CustoUnitario = produto.PrecoCompra,
                    Desconto = 0
                });
            }

            AtualizarGridCarrinho();

            BuscaProdutoTextBox.Clear();
            _viewModel.Produtos.Clear();
        }

    }
}
