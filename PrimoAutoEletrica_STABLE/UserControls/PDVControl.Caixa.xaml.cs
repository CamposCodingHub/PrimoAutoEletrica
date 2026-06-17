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
        // Abertura, suprimento, sangria e fechamento de caixa.

        private void AbrirCaixaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CAIXA_ABRIR", "Voce nao possui permissao para abrir o caixa."))
            {
                return;
            }

            if (_caixaService.ObterSessaoAbertaAtual() != null)
            {
                ExibirMensagem(
                    "Ja existe um caixa aberto para este operador.",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AtualizarEstadoCaixa();
                return;
            }

            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Abrir caixa",
                Header = "Abertura de caixa",
                Subheader = "Informe o valor inicial que entra no caixa nesta sessao operacional.",
                ValorLabel = "Valor de abertura",
                ConfirmButtonText = "Abrir caixa",
                PermitirZero = true
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var sessao = _caixaService.AbrirCaixa(dialog.ValorInformado, dialog.ObservacoesInformadas);

                AtualizarEstadoCaixa();

                ExibirMensagem(
                    $"Caixa {sessao.NumeroCaixa} aberto com sucesso.\nSaldo inicial: {sessao.ValorAbertura:C}",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SuprimentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CAIXA_SUPRIMENTO", "Voce nao possui permissao para registrar suprimento."))
            {
                return;
            }

            if (!GarantirSessaoCaixaAberta())
            {
                return;
            }

            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Suprimento de caixa",
                Header = "Suprimento operacional",
                Subheader = "Registre o valor que esta entrando no caixa fora do fluxo de venda.",
                ValorLabel = "Valor do suprimento",
                ConfirmButtonText = "Registrar suprimento"
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var sessao = _caixaService.RegistrarSuprimento(
                    dialog.ValorInformado,
                    dialog.ObservacoesInformadas,
                    _viewModel.FormaPagamentoSelecionada);

                AtualizarEstadoCaixa();

                ExibirMensagem(
                    $"Suprimento registrado.\nSaldo esperado atual: {sessao.ValorEsperado:C}",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SangriaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CAIXA_SANGRIA", "Voce nao possui permissao para registrar sangria."))
            {
                return;
            }

            var sessao = _caixaService.ObterSessaoAbertaAtual();

            if (sessao == null)
            {
                ExibirMensagem(
                    "Abra o caixa antes de registrar uma sangria.",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                AtualizarEstadoCaixa();
                return;
            }

            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Sangria de caixa",
                Header = "Sangria operacional",
                Subheader = $"Saldo esperado atual: {sessao.ValorEsperado:C}\nInforme o valor que sera retirado do caixa.",
                ValorLabel = "Valor da sangria",
                ConfirmButtonText = "Registrar sangria",
                ObservacoesObrigatorias = true
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var sessaoAtualizada = _caixaService.RegistrarSangria(
                    dialog.ValorInformado,
                    dialog.ObservacoesInformadas,
                    _viewModel.FormaPagamentoSelecionada);

                AtualizarEstadoCaixa();

                ExibirMensagem(
                    $"Sangria registrada.\nSaldo esperado atual: {sessaoAtualizada.ValorEsperado:C}",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void FecharCaixaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CAIXA_FECHAR", "Voce nao possui permissao para fechar o caixa."))
            {
                return;
            }

            var sessao = _caixaService.ObterSessaoAbertaAtual();

            if (sessao == null)
            {
                ExibirMensagem(
                    "Nenhum caixa aberto foi encontrado para fechamento.",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AtualizarEstadoCaixa();
                return;
            }

            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Fechar caixa",
                Header = "Fechamento de caixa",
                Subheader = $"Valor esperado no sistema: {sessao.ValorEsperado:C}\nInforme o valor contado em caixa.",
                ValorLabel = "Valor contado",
                ConfirmButtonText = "Avancar para fechamento",
                PermitirZero = true,
                ValorInicial = sessao.ValorEsperado
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var diferenca = dialog.ValorInformado - sessao.ValorEsperado;

            var confirmar = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Fechar caixa",
                    Header = "Confirmacao de fechamento",
                    Summary = $"Voce esta prestes a fechar o caixa {sessao.NumeroCaixa}.",
                    Details = $"Valor esperado: {sessao.ValorEsperado:C}\nValor informado: {dialog.ValorInformado:C}\nDiferenca: {diferenca:C}\nVendas da sessao: {sessao.QuantidadeVendas}",
                    Impact = "Depois do fechamento, o PDV volta a bloquear novas vendas ate uma nova abertura de caixa.",
                    Keyword = "FECHAR",
                    ConfirmButtonText = "Fechar caixa agora"
                });

            if (!confirmar)
            {
                return;
            }

            try
            {
                var sessaoFechada = _caixaService.FecharCaixa(dialog.ValorInformado, dialog.ObservacoesInformadas);

                AtualizarEstadoCaixa();

                ExibirMensagem(
                    $"Caixa fechado com sucesso.\nValor esperado: {sessaoFechada.ValorEsperado:C}\nValor informado: {sessaoFechada.ValorInformadoFechamento.GetValueOrDefault():C}\nDiferenca: {sessaoFechada.DiferencaFechamento:C}",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

    }
}
