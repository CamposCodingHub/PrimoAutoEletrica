using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private static readonly string[] DeepQaModules =
        {
            "Dashboard",
            "Clientes",
            "Veiculos",
            "AutoEletricaTecnica",
            "Orcamentos",
            "OrdensServico",
            "OficinaKanban",
            "PDV",
            "Estoque",
            "CatalogoPecas",
            "ImportarNFe",
            "Financeiro",
            "Fornecedores",
            "Funcionarios",
            "Agendamentos",
            "Relatorios",
            "Help"
        };

        private void RunDeepQaChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "DeepQa:LongRunNavegacaoTema", () =>
            {
                var themeService = new ThemeService();
                var temaOriginal = themeService.GetCurrentTheme();
                MainWindow? window = null;
                var ciclos = 2;
                var navegacoes = 0;
                var sw = Stopwatch.StartNew();

                try
                {
                    themeService.ApplyTheme(AppTheme.Light);
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    for (var ciclo = 1; ciclo <= ciclos; ciclo++)
                    {
                        var temaCiclo = ciclo % 2 == 1 ? AppTheme.Light : AppTheme.Dark;
                        themeService.ApplyTheme(temaCiclo);
                        WaitForUiIdle();

                        foreach (var modulo in DeepQaModules)
                        {
                            var ok = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                                ? window.OpenImportarNFeForAutomation()
                                : window.NavigateToModuleForAutomation(modulo, forceReload: true);

                            if (!ok || window.CurrentContentElement == null)
                            {
                                throw new InvalidOperationException(
                                    $"DeepQa ciclo {ciclo}: falha ao abrir modulo {modulo}.");
                            }

                            navegacoes++;
                            WaitForUiIdle();
                            ValidarTemaAplicadoBasico(window);

                            var content = window.CurrentContentElement as FrameworkElement
                                ?? throw new InvalidOperationException($"DeepQa: conteudo nulo em {modulo}.");

                            var anyButton = FindVisualChildren<Button>(content).Any();
                            if (!anyButton && !string.Equals(modulo, "Help", StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidOperationException(
                                    $"DeepQa: modulo {modulo} sem botoes no visual tree.");
                            }

                            var grid = FindVisualChildren<DataGrid>(content).FirstOrDefault();
                            if (grid != null && grid.Items.Count > 0)
                            {
                                grid.SelectedIndex = 0;
                            }
                        }

                        if (!window.NavigateToModuleForAutomation("Dashboard", forceReload: true))
                        {
                            throw new InvalidOperationException($"DeepQa ciclo {ciclo}: retorno ao Dashboard falhou.");
                        }

                        navegacoes++;
                    }

                    sw.Stop();
                    if (navegacoes < DeepQaModules.Length * ciclos)
                    {
                        throw new InvalidOperationException("DeepQa: contagem de navegacoes abaixo do esperado.");
                    }

                    App.Logger.LogInfo(
                        $"DeepQa long-run: {ciclos} ciclos, {navegacoes} navegacoes, {sw.Elapsed.TotalSeconds:F1}s.",
                        "Smoke");
                }
                finally
                {
                    themeService.ApplyTheme(temaOriginal);
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }
                }
            });

            RunCheck(result, "DeepQa:CapturasVisuaisLightDark", () =>
            {
                var themeService = new ThemeService();
                var temaOriginal = themeService.GetCurrentTheme();
                MainWindow? window = null;
                var outDir = Path.Combine(AppContext.BaseDirectory, "Logs", "qa-visual", "fase11-deep");
                Directory.CreateDirectory(outDir);

                var prioridade = new[]
                {
                    "Dashboard",
                    "Funcionarios",
                    "Clientes",
                    "Veiculos",
                    "OrdensServico",
                    "Agendamentos",
                    "Estoque",
                    "Financeiro",
                    "Relatorios",
                    "Fornecedores",
                    "PDV",
                    "Orcamentos"
                };

                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    window.Width = 1366;
                    window.Height = 768;
                    WaitForUiIdle();

                    foreach (var tema in new[] { AppTheme.Light, AppTheme.Dark })
                    {
                        themeService.ApplyTheme(tema);
                        WaitForUiIdle();

                        foreach (var modulo in prioridade)
                        {
                            if (!window.NavigateToModuleForAutomation(modulo, forceReload: true) ||
                                window.CurrentContentElement == null)
                            {
                                throw new InvalidOperationException(
                                    $"DeepQa visual: falha ao abrir {modulo} em {tema}.");
                            }

                            WaitForUiIdle();
                            var content = window.CurrentContentElement as FrameworkElement
                                ?? throw new InvalidOperationException($"DeepQa visual: conteudo invalido {modulo}.");

                            var file = Path.Combine(outDir, $"{modulo}-{tema}-1366x768.png".ToLowerInvariant());
                            CaptureElementPng(content, file);
                            if (!File.Exists(file) || new FileInfo(file).Length < 1024)
                            {
                                throw new InvalidOperationException($"DeepQa visual: captura invalida {file}.");
                            }
                        }
                    }
                }
                finally
                {
                    themeService.ApplyTheme(temaOriginal);
                    if (window?.IsVisible == true)
                    {
                        window.Close();
                    }
                }
            });

            RunCheck(result, "DeepQa:FuncionariosPainelEBotoes", () =>
            {
                var hostWindow = CreateHostWindow(new FuncionariosControl(), nameof(FuncionariosControl));
                try
                {
                    ShowWindowForInteraction(hostWindow);
                    if (hostWindow.Content is not FuncionariosControl control)
                    {
                        throw new InvalidOperationException("DeepQa Funcionarios: host sem controle.");
                    }

                    PrepareInteractiveSurface(control, typeof(FuncionariosControl));
                    WaitForUiIdle();

                    var dataGrid = FindElementByName<DataGrid>(control, "FuncionariosDataGrid")
                        ?? throw new InvalidOperationException("DeepQa Funcionarios: grade ausente.");

                    WaitForCondition(
                        () => dataGrid.Items.Count > 0,
                        TimeSpan.FromSeconds(8),
                        "DeepQa Funcionarios: grade vazia apos carga.");

                    dataGrid.SelectedIndex = 0;
                    control.UpdateLayout();
                    PumpDispatcher();
                    WaitForUiIdle();

                    if (control.UltimoPainelOperacional == null)
                    {
                        throw new InvalidOperationException("DeepQa Funcionarios: painel operacional nao carregou.");
                    }

                    var required =
                        new[]
                        {
                            "AtualizarButton",
                            "NovoFuncionarioButton",
                            "BloquearFuncionarioButton",
                            "ReativarFuncionarioButton",
                            "GerenciarPerfisButton",
                            "ConfigurarPermissoesButton"
                        };

                    foreach (var name in required)
                    {
                        if (FindElementByName<Button>(control, name) == null)
                        {
                            throw new InvalidOperationException($"DeepQa Funcionarios: botao {name} ausente.");
                        }
                    }

                    var search = FindElementByName<TextBox>(control, "SearchTextBox")
                        ?? throw new InvalidOperationException("DeepQa Funcionarios: SearchTextBox ausente.");
                    search.Text = "___sem_resultado_smoke___";
                    WaitForUiIdle();
                    if (dataGrid.Items.Count != 0)
                    {
                        throw new InvalidOperationException("DeepQa Funcionarios: busca inexistente nao esvaziou a grade.");
                    }

                    search.Text = string.Empty;
                    WaitForUiIdle();
                    if (dataGrid.Items.Count == 0)
                    {
                        throw new InvalidOperationException("DeepQa Funcionarios: limpar busca nao restaurou itens.");
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

        private static void ValidarTemaAplicadoBasico(MainWindow window)
        {
            var bg = window.TryFindResource("AppBackgroundBrush") as SolidColorBrush
                ?? throw new InvalidOperationException("AppBackgroundBrush ausente apos troca de tema.");
            var text = window.TryFindResource("PrimaryTextBrush") as SolidColorBrush
                ?? throw new InvalidOperationException("PrimaryTextBrush ausente apos troca de tema.");

            // Contraste basico: fundo e texto nao podem ser iguais.
            if (bg.Color == text.Color)
            {
                throw new InvalidOperationException("Tema aplicado com fundo e texto na mesma cor.");
            }
        }
    }
}
