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
        // Visualizacao, remocao, quantidade e desconto do carrinho.

        private void VisualizarItemCarrinhoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ItemVenda item)
            {
                return;
            }

            var produto = item.Produto;

            var detalhes =
                $"Descrição: {item.NomeExibicao}\n" +
                $"Tipo: {item.Tipo}\n" +
                $"Quantidade: {item.Quantidade}\n" +
                $"Preço unitário: {item.PrecoUnitario:C}\n" +
                $"Custo unitário: {item.CustoUnitario:C}\n" +
                $"Desconto do item: {item.Desconto:C}\n" +
                $"Subtotal: {item.Subtotal:C}";

            if (produto != null)
            {
                detalhes +=
                    "\n\n--- Produto ---\n" +
                    $"Código: {produto.Codigo}\n" +
                    $"SKU: {produto.SKU}\n" +
                    $"Código de barras: {produto.CodigoBarras}\n" +
                    $"Categoria: {produto.Categoria}\n" +
                    $"Marca: {produto.Marca}\n" +
                    $"Fornecedor: {produto.Fornecedor}\n" +
                    $"Estoque disponível: {produto.QuantidadeDisponivel}\n" +
                    $"Preço venda atual: {produto.PrecoVenda:C}";
            }

            ExibirMensagem(
                detalhes,
                "Detalhes do item",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RemoverItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ItemVenda item)
            {
                RemoverItemDoCarrinho(item);
            }
        }

        private void AumentarQuantidadeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ItemVenda item)
            {
                if (!item.UsaEstoque || item.Produto == null)
                {
                    item.Quantidade++;
                    AtualizarGridCarrinho();
                    return;
                }

                var disponibilidade = ObterDisponibilidadeOperacional(item.Produto);

                if (item.Quantidade + 1 > disponibilidade)
                {
                    ExibirMensagem(
                        $"Disponibilidade operacional insuficiente. Restante: {Math.Max(0, disponibilidade - item.Quantidade)}.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                item.Quantidade++;
                AtualizarGridCarrinho();
            }
        }

        private void DiminuirQuantidadeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ItemVenda item)
            {
                if (item.Quantidade > 1)
                {
                    item.Quantidade--;
                    AtualizarGridCarrinho();
                    return;
                }

                RemoverItemDoCarrinho(item);
            }
        }

        private void RemoverItemSelecionado()
        {
            if (CarrinhoListView.SelectedItem is ItemVenda item)
            {
                RemoverItemDoCarrinho(item);
            }
        }

        private void RemoverItemDoCarrinho(ItemVenda item)
        {
            var resultado = ExibirMensagem(
                $"Deseja remover {item.NomeExibicao} do carrinho?",
                "Remover Item",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                _viewModel.Carrinho.Remove(item);
                AtualizarGridCarrinho();
            }
        }

        private void AplicarDescontoButton_Click(object sender, RoutedEventArgs e)
        {
            AplicarDesconto();
        }

        private void DescontoTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AplicarDesconto();
                e.Handled = true;
            }
        }

        private void AplicarDesconto()
        {
            if (!ValidarPermissao("PDV_APLICAR_DESCONTO", "Voce nao possui permissao para aplicar descontos no PDV."))
            {
                return;
            }

            var descontoAnterior = _viewModel.DescontoGeral;

            if (TryParseDecimalFlexible(DescontoTextBox.Text, out var desconto))
            {
                try
                {
                    ComercialValidationHelper.GarantirValorMaiorOuIgualZero(desconto, "o desconto");
                    ComercialValidationHelper.GarantirDescontoValido(desconto, _viewModel.Subtotal, "O desconto");
                }
                catch (InvalidOperationException ex)
                {
                    ExibirMensagem(ex.Message, "Desconto", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _viewModel.DescontoGeral = desconto;
                _viewModel.AtualizarTotal();

                global::PrimoAutoEletrica.App.Audit.Registrar(
                    categoria: "PDV",
                    acao: "DescontoAplicado",
                    entidade: "Carrinho",
                    entidadeId: global::PrimoAutoEletrica.App.Session.SessionId.ToString(),
                    detalhes: $"Subtotal={_viewModel.Subtotal:C}; Total={_viewModel.Total:C}",
                    valorAnterior: descontoAnterior.ToString("F2"),
                    valorNovo: desconto.ToString("F2"));

                ExibirMensagem(
                    $"Desconto de R$ {desconto:F2} aplicado!",
                    "Desconto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            ExibirMensagem(
                "Valor de desconto invalido!",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        private static bool TextoContem(string? origem, string termo)
        {
            return !string.IsNullOrWhiteSpace(origem)
                && origem.ToUpperInvariant().Contains(termo);
        }

        private static bool TryParseDecimalFlexible(string? text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

    }
}
