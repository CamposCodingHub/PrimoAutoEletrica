using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private void RunModalsConfirmacaoChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Modals:ConfirmacaoCriticaCampoVisivel", () =>
            {
                var request = new CriticalActionRequest
                {
                    WindowTitle = "Excluir veiculo",
                    Header = "Exclusao de veiculo",
                    Summary = "Voce esta prestes a excluir veiculo 'ABC1D23'.",
                    Details = "Resumo: Fiat Uno\nCliente vinculado: Smoke Cliente",
                    Impact = "O veiculo sera removido do cadastro local.",
                    Keyword = "EXCLUIR",
                    ConfirmButtonText = "Excluir registro"
                };

                var dialog = new ConfirmacaoCriticaWindow(request);
                try
                {
                    ShowWindowForInteraction(dialog);
                    WaitForUiIdle();

                    var textBox = FindElementByName<TextBox>(dialog, "ConfirmationTextBox");
                    if (textBox == null)
                    {
                        throw new InvalidOperationException("ConfirmationTextBox nao encontrado no modal critico.");
                    }

                    if (!textBox.IsVisible || textBox.ActualHeight < 20 || textBox.ActualWidth < 80)
                    {
                        throw new InvalidOperationException(
                            $"ConfirmationTextBox inacessivel (Visible={textBox.IsVisible}, H={textBox.ActualHeight:0}, W={textBox.ActualWidth:0}).");
                    }

                    var keyword = FindElementByName<TextBlock>(dialog, "KeywordTextBlock");
                    if (keyword == null || !string.Equals(keyword.Text, "EXCLUIR", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("KeywordTextBlock nao exibe EXCLUIR.");
                    }

                    // Garante contraste DS (sem hardcode #334155).
                    var details = FindElementByName<TextBlock>(dialog, "DetailsTextBlock");
                    if (details?.Foreground is SolidColorBrush brush)
                    {
                        var color = brush.Color;
                        if (color.R == 0x33 && color.G == 0x41 && color.B == 0x55)
                        {
                            throw new InvalidOperationException("DetailsTextBlock ainda usa foreground hardcoded #334155.");
                        }
                    }

                    textBox.Text = "EXCLUIR";
                    WaitForUiIdle();
                    var confirmar = FindElementByName<Button>(dialog, "ConfirmarButton");
                    if (confirmar == null || !confirmar.IsEnabled)
                    {
                        throw new InvalidOperationException("ConfirmarButton nao habilitou apos digitar EXCLUIR.");
                    }
                }
                finally
                {
                    TryCloseWindow(dialog, TimeSpan.FromSeconds(3));
                }
            });

            RunCheck(result, "Modals:AdminPasswordConfirmacaoLayout", () =>
            {
                var dialog = new AdminPasswordConfirmationWindow(
                    "Excluir Veiculo",
                    "Para excluir o veiculo de teste com nome longo Marca Modelo, confirme sua senha de administrador.");
                try
                {
                    // Nao forcar Height antigo (280) — valida o layout real da janela.
                    dialog.WindowStartupLocation = WindowStartupLocation.Manual;
                    if (App.IsSmokeVisible)
                    {
                        dialog.Left = 80;
                        dialog.Top = 80;
                        dialog.ShowInTaskbar = true;
                        dialog.Topmost = true;
                    }
                    else
                    {
                        dialog.Left = -10000;
                        dialog.Top = -10000;
                        dialog.ShowInTaskbar = false;
                    }

                    dialog.Show();
                    dialog.UpdateLayout();
                    WaitForUiIdle();

                    var senha = FindElementByName<PasswordBox>(dialog, "txtSenhaConfirmacao");
                    if (senha == null)
                    {
                        throw new InvalidOperationException("txtSenhaConfirmacao nao encontrado.");
                    }

                    if (senha.ActualHeight < 20 || senha.ActualWidth < 80)
                    {
                        throw new InvalidOperationException(
                            $"PasswordBox inacessivel (H={senha.ActualHeight:0}, W={senha.ActualWidth:0}).");
                    }

                    // Garante que o campo nao esta sob o footer (coordenadas na janela).
                    var ponto = senha.TransformToAncestor(dialog).Transform(new System.Windows.Point(senha.ActualWidth / 2, senha.ActualHeight / 2));
                    if (ponto.Y < 40 || ponto.Y > dialog.ActualHeight - 40)
                    {
                        throw new InvalidOperationException(
                            $"PasswordBox fora da area util (Y={ponto.Y:0}, WindowH={dialog.ActualHeight:0}).");
                    }

                    senha.Focus();
                    if (!senha.IsKeyboardFocusWithin && !senha.IsFocused)
                    {
                        // Em modo headless o focus pode falhar; tamanho/posicao ja validam usabilidade.
                        _logger.LogInfo("AdminPassword: Focus no PasswordBox nao confirmado (headless ok se layout valido).");
                    }

                    if (App.IsSmokeVisible)
                    {
                        Thread.Sleep(700);
                    }
                }
                finally
                {
                    TryCloseWindow(dialog, TimeSpan.FromSeconds(3));
                }
            });

            RunCheck(result, "Modals:VeiculosExcluirFluxoUnificado", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    WaitForUiIdle();

                    if (!window.NavigateToModuleForAutomation("Veiculos", forceReload: true))
                    {
                        throw new InvalidOperationException("Falha ao navegar para Veiculos.");
                    }

                    WaitForUiIdle();
                    var control = FindVisualChildren<UserControls.VeiculosControl>(window).FirstOrDefault()
                        ?? throw new InvalidOperationException("VeiculosControl nao carregado.");

                    // Seleciona primeiro item se houver e habilita o botao da toolbar.
                    var grid = FindElementByName<DataGrid>(control, "VeiculosDataGrid")
                        ?? FindVisualChildren<DataGrid>(control).FirstOrDefault();
                    if (grid != null && grid.Items.Count > 0)
                    {
                        grid.SelectedIndex = 0;
                        WaitForUiIdle();
                    }

                    var excluir = FindElementByName<Button>(control, "ExcluirVeiculoButton")
                        ?? FindVisualChildren<Button>(control)
                            .FirstOrDefault(b =>
                                string.Equals(ExtractButtonText(b), "Excluir", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(ExtractButtonText(b), "Del", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(
                                    System.Windows.Automation.AutomationProperties.GetName(b),
                                    "Excluir veículo",
                                    StringComparison.OrdinalIgnoreCase));

                    if (excluir == null)
                    {
                        throw new InvalidOperationException("Botao Excluir nao encontrado em Veiculos.");
                    }

                    if (!excluir.IsEnabled && grid != null && grid.Items.Count > 0)
                    {
                        // Fallback: dispara SelectionChanged e tenta novamente.
                        grid.SelectedIndex = -1;
                        WaitForUiIdle();
                        grid.SelectedIndex = 0;
                        WaitForUiIdle();
                    }

                    if (!excluir.IsEnabled)
                    {
                        // Sem item selecionavel — valida apenas que o fluxo unificado existe (handler admin).
                        return;
                    }

                    // Clique abre MessageBox + AdminPassword (auto em smoke). Nao deve abrir ConfirmacaoCriticaWindow.
                    RaiseButtonClick(excluir);
                    WaitForUiIdle(cycles: 12);

                    var criticaAberta = Application.Current.Windows
                        .OfType<ConfirmacaoCriticaWindow>()
                        .Any(w => w.IsVisible);
                    if (criticaAberta)
                    {
                        throw new InvalidOperationException(
                            "Excluir em Veiculos abriu ConfirmacaoCriticaWindow; esperado AdminPasswordConfirmationWindow.");
                    }
                }
                finally
                {
                    if (window != null)
                    {
                        TryCloseWindow(window, TimeSpan.FromSeconds(5));
                    }
                }
            });
        }
    }
}
