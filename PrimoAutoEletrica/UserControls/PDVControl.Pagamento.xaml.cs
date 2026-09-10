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
        // Pagamento, forma final e registro da venda.

        private void PagamentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Carrinho.Count == 0)
            {
                ExibirMensagem(
                    "Carrinho vazio!",
                    UiText.T("Warning"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var sessaoCaixa = GarantirCaixaAbertoParaVenda();

            if (sessaoCaixa == null)
            {
                return;
            }

            var total = _viewModel.Total;

            if (total <= 0)
            {
                ExibirMensagem(
                    "O total da venda precisa ser maior que zero.",
                    "Pagamento",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var formaPagamento = ResolverFormaPagamentoFinal(total);

            if (string.IsNullOrWhiteSpace(formaPagamento))
            {
                return;
            }

            var confirmarPagamento = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Finalizar venda",
                    Header = "Confirmacao de pagamento",
                    Summary = "Voce esta prestes a concluir a venda atual do PDV.",
                    Details = $"Forma de pagamento: {formaPagamento}\nItens: {_viewModel.Carrinho.Count}\nCliente: {_viewModel.ClienteSelecionado?.Nome ?? "Consumidor final"}\nTotal a pagar: {total:C}",
                    Impact = "A venda sera gravada com atualizacao de estoque, caixa e financeiro desta sessao.",
                    Keyword = "PAGAR",
                    ConfirmButtonText = "Finalizar venda"
                });

            if (confirmarPagamento)
            {
                FinalizarVenda(total, sessaoCaixa, formaPagamento);
            }
        }

        private string? ResolverFormaPagamentoFinal(decimal total)
        {
            if (!string.Equals(_viewModel.FormaPagamentoSelecionada, "Misto", StringComparison.OrdinalIgnoreCase))
            {
                return _viewModel.FormaPagamentoSelecionada;
            }

            if (!ValidarPermissao("PDV_PAGAMENTO_MISTO", "Voce nao possui permissao para finalizar vendas com pagamento misto."))
            {
                return null;
            }

            if (App.IsAutomatedTestMode)
            {
                var primeiraParte = decimal.Round(total / 2m, 2);
                var segundaParte = total - primeiraParte;
                var resumoAutomacao = $"Misto: Dinheiro {primeiraParte:C} | PIX {segundaParte:C}";

                _viewModel.PagamentoMistoResumo = resumoAutomacao;

                App.Logger.LogInfo(
                    $"Pagamento misto validado em automacao: {resumoAutomacao}.",
                    "PDV");

                return resumoAutomacao;
            }

            var dialog = new PagamentoMistoWindow(total);
            WindowOwnerHelper.ConfigureOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return null;
            }

            _viewModel.PagamentoMistoResumo = dialog.ResumoPagamento;
            return dialog.ResumoPagamento;
        }

        private void FinalizarVenda(decimal total, CaixaSessaoOperacional sessaoCaixa, string formaPagamento)
        {
            if (!ValidarPermissao("PDV_REGISTRAR_VENDA", "Voce nao possui permissao para finalizar vendas no PDV."))
            {
                return;
            }

            try
            {
                _viewModel.AtualizarTotal();

                var descontoAplicado = Math.Min(_viewModel.DescontoGeral, _viewModel.Subtotal);

                if (descontoAplicado != _viewModel.DescontoGeral)
                {
                    _viewModel.DescontoGeral = descontoAplicado;
                    _viewModel.AtualizarTotal();
                }

                total = _viewModel.Total;

                if (total <= 0)
                {
                    ExibirMensagem(
                        "O total da venda precisa ser maior que zero.",
                        "Pagamento",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var venda = new Venda
                {
                    Data = DateTime.Now,
                    Cliente = _viewModel.ClienteSelecionado,
                    Itens = new List<ItemVenda>(_viewModel.Carrinho),
                    Total = total,
                    Desconto = _viewModel.DescontoGeral,
                    FormaPagamento = formaPagamento,
                    Usuario = global::PrimoAutoEletrica.App.Session.UserName,
                    CaixaSessaoId = sessaoCaixa.Id
                };

                _vendaService.RegistrarVenda(venda);
                _viewModel.UltimaVendaFinalizadaId = venda.Id;

                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "PDV",
                    "VendaFinalizadaTela",
                    "Venda",
                    venda.Id.ToString(),
                    $"Total={total:C}; Itens={venda.Itens.Count}; FormaPagamento={venda.FormaPagamento}; Caixa={sessaoCaixa.NumeroCaixa}");

                ExibirMensagem(
                    $"Venda realizada com sucesso!\nForma de pagamento: {venda.FormaPagamento}\nTotal: R$ {total:F2}",
                    UiText.T("Success"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LimparCarrinho();
                CarregarDadosIniciais();
                AtualizarEstadoCaixa();
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao finalizar venda no PDV.", ex);

                ExibirMensagem(
                    $"Erro ao finalizar venda: {ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

    }
}
