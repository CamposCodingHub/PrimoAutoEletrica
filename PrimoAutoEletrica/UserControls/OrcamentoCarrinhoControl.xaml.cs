using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class OrcamentoCarrinhoControl : UserControl
    {
        private OrcamentosViewModel? _viewModel;

        public OrcamentoCarrinhoControl()
        {
            InitializeComponent();
        }

        public void SetViewModel(OrcamentosViewModel viewModel)
        {
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void EditarItem_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItemFromSender(sender);
            if (_viewModel == null || item == null)
            {
                return;
            }

            var produto = item.ProdutoId.HasValue
                ? _viewModel.ObterProdutoPorId(item.ProdutoId.Value)
                : null;
            produto ??= CriarProdutoTemporario(item);
            var quantidadeWindow = new QuantidadeProdutoWindow(produto, item.Quantidade);
            WindowOwnerHelper.ConfigureOwner(quantidadeWindow, this);

            if (quantidadeWindow.ShowDialog() != true || quantidadeWindow.Quantidade <= 0)
            {
                return;
            }

            item.Quantidade = quantidadeWindow.Quantidade;
            item.Subtotal = item.Quantidade * item.PrecoUnitario;
            item.LucroEstimado = (item.PrecoUnitario - item.PrecoCusto) * item.Quantidade;
            item.MargemLucro = item.PrecoUnitario > 0 ? ((item.PrecoUnitario - item.PrecoCusto) / item.PrecoUnitario) * 100 : 0;
            _viewModel.CalcularTotais();
        }

        private void RemoverItem_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItemFromSender(sender);
            if (_viewModel == null || item == null)
            {
                return;
            }

            _viewModel.RemoverItemDoCarrinho(item);
        }

        private void DuplicarItem_Click(object sender, RoutedEventArgs e)
        {
            var item = GetItemFromSender(sender);
            if (_viewModel == null || item == null)
            {
                return;
            }

            if (item.UsaEstoque)
            {
                var produto = item.ProdutoId.HasValue
                    ? _viewModel.ObterProdutoPorId(item.ProdutoId.Value)
                    : null;
                produto ??= CriarProdutoTemporario(item);
                _viewModel.AdicionarItemAoCarrinho(produto, item.Quantidade);
                return;
            }

            _viewModel.ItensCarrinho.Add(new OrcamentoItem
            {
                Id = Guid.NewGuid(),
                OrcamentoId = _viewModel.OrcamentoAtual?.Id ?? Guid.NewGuid(),
                Tipo = item.Tipo,
                ProdutoNome = item.ProdutoNome,
                ProdutoCodigo = item.ProdutoCodigo,
                ProdutoCategoria = item.ProdutoCategoria,
                ProdutoMarca = item.ProdutoMarca,
                ProdutoAplicacao = item.ProdutoAplicacao,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                PrecoCusto = item.PrecoCusto,
                Desconto = item.Desconto,
                EstoqueDisponivel = 0,
                Observacoes = item.Observacoes
            });
            _viewModel.CalcularTotais();
        }

        private static OrcamentoItem? GetItemFromSender(object sender)
        {
            return sender is FrameworkElement element ? element.Tag as OrcamentoItem : null;
        }

        private static Produto CriarProdutoTemporario(OrcamentoItem item)
        {
            return new Produto
            {
                Id = item.ProdutoId ?? Guid.NewGuid(),
                Nome = item.ProdutoNome,
                Codigo = item.ProdutoCodigo,
                Categoria = item.ProdutoCategoria,
                Marca = item.ProdutoMarca,
                QuantidadeEstoque = Math.Max(item.EstoqueDisponivel, item.Quantidade),
                PrecoCompra = item.PrecoCusto,
                PrecoVenda = item.PrecoUnitario,
                MargemLucro = item.MargemLucro,
                Ativo = true
            };
        }
    }
}
