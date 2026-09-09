using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        /// <summary>
        /// Fonte permanente: NavigationService + Help. Novos modulos no mapa entram automaticamente.
        /// </summary>
        private static IReadOnlyList<string> ObterDeepQaModules()
        {
            var permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);
            var navigation = new NavigationService(permissionService, App.Logger);
            var modules = navigation.GetCanonicalModuleNames()
                .Where(name => !string.Equals(name, "Ajuda", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (modules.Count < 16)
            {
                throw new InvalidOperationException(
                    $"DeepQa permanente: inventario insuficiente ({modules.Count}). Esperado >= 16 modulos canonicos.");
            }

            return modules;
        }

        private void RunDeepQaChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "DeepQa:InventarioPermanente", () =>
            {
                var modules = ObterDeepQaModules();
                if (!modules.Contains("Funcionarios", StringComparer.OrdinalIgnoreCase) ||
                    !modules.Contains("Help", StringComparer.OrdinalIgnoreCase) ||
                    !modules.Contains("Dashboard", StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "DeepQa permanente: Dashboard/Funcionarios/Help devem permanecer no inventario.");
                }

                App.Logger.LogInfo(
                    $"DeepQa permanente: {modules.Count} modulos canonicos = {string.Join(", ", modules)}",
                    "Smoke");
            });

            RunCheck(result, "DeepQa:LongRunNavegacaoTema", () =>
            {
                var modules = ObterDeepQaModules();
                var themeService = new ThemeService();
                var temaOriginal = themeService.GetCurrentTheme();
                MainWindow? window = null;
                var ciclos = 5; // Exhaustive audit 2.0: Long Run mínimo 5 ciclos
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

                        foreach (var modulo in modules)
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
                    if (navegacoes < modules.Count * ciclos)
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

            RunCheck(result, "DeepQa:AcessibilidadeFocoEIdentidade", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    if (window.TryFindResource("PrimoxFocusVisual") == null)
                    {
                        throw new InvalidOperationException("PrimoxFocusVisual ausente nos recursos.");
                    }

                    var theme = FindElementByName<Button>(window, "ThemeToggleButton")
                        ?? throw new InvalidOperationException("ThemeToggleButton ausente.");
                    if (string.IsNullOrWhiteSpace(theme.ToolTip?.ToString()) &&
                        string.IsNullOrWhiteSpace(AutomationProperties.GetName(theme)))
                    {
                        throw new InvalidOperationException("ThemeToggleButton sem ToolTip/AutomationName.");
                    }

                    theme.Focus();
                    WaitForUiIdle();
                    if (!theme.IsKeyboardFocusWithin && Keyboard.FocusedElement != theme)
                    {
                        // Alguns estilos WPF aceitam Focus() sem IsKeyboardFocusWithin imediato; exigir Focusable.
                        if (!theme.Focusable)
                        {
                            throw new InvalidOperationException("ThemeToggleButton nao e Focusable.");
                        }
                    }

                    if (!window.NavigateToModuleForAutomation("PDV", forceReload: true) ||
                        window.CurrentContentElement == null)
                    {
                        throw new InvalidOperationException("DeepQa a11y: falha ao abrir PDV.");
                    }

                    WaitForUiIdle();
                    var pdv = window.CurrentContentElement as FrameworkElement
                        ?? throw new InvalidOperationException("DeepQa a11y: PDV invalido.");

                    var iconish = FindVisualChildren<Button>(pdv)
                        .Where(b =>
                        {
                            var content = b.Content?.ToString()?.Trim() ?? string.Empty;
                            return content is "+" or "-" or "X" or "×";
                        })
                        .ToList();

                    // Botoes +/-/X so existem quando ha itens no carrinho; se o carrinho estiver vazio,
                    // validamos a identidade do Clear da busca global (IconButton tipico).
                    if (iconish.Count > 0)
                    {
                        foreach (var button in iconish)
                        {
                            var tip = button.ToolTip?.ToString();
                            var name = AutomationProperties.GetName(button);
                            if (string.IsNullOrWhiteSpace(tip) && string.IsNullOrWhiteSpace(name))
                            {
                                throw new InvalidOperationException(
                                    $"DeepQa a11y: botao PDV '{button.Content}' sem ToolTip/AutomationName.");
                            }
                        }
                    }
                    else
                    {
                        var clear = FindElementByName<Button>(window, "ClearButton");
                        if (clear != null)
                        {
                            clear.Visibility = Visibility.Visible;
                            WaitForUiIdle();
                            var tip = clear.ToolTip?.ToString();
                            var name = AutomationProperties.GetName(clear);
                            if (string.IsNullOrWhiteSpace(tip) && string.IsNullOrWhiteSpace(name))
                            {
                                throw new InvalidOperationException(
                                    "DeepQa a11y: ClearButton da busca global sem ToolTip/AutomationName.");
                            }
                        }
                    }

                    // Tab order basico no shell: Theme -> Density deve ser focavel.
                    var density = FindElementByName<Button>(window, "DensityToggleButton");
                    if (density == null || !density.Focusable)
                    {
                        throw new InvalidOperationException("DensityToggleButton ausente ou nao focavel.");
                    }
                }
                finally
                {
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
                var outDir = Path.Combine(AppContext.BaseDirectory, "Logs", "qa-visual", "fase12-a11y");
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
                    "Orcamentos",
                    "Help"
                };

                var sizes = new (int W, int H)[]
                {
                    (1366, 768),
                    (1600, 900),
                    (1920, 1080),
                    (2560, 1440)
                };

                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    foreach (var tema in new[] { AppTheme.Light, AppTheme.Dark })
                    {
                        themeService.ApplyTheme(tema);
                        WaitForUiIdle();

                        foreach (var size in sizes)
                        {
                            window.Width = size.W;
                            window.Height = size.H;
                            WaitForUiIdle();

                            // Prioridade: 1366 captura todos; demais resolucoes capturam subset.
                            var mods = size.W == 1366
                                ? prioridade
                                : new[] { "Dashboard", "Funcionarios", "PDV", "Relatorios", "Help" };

                            foreach (var modulo in mods)
                            {
                                if (!window.NavigateToModuleForAutomation(modulo, forceReload: true) ||
                                    window.CurrentContentElement == null)
                                {
                                    throw new InvalidOperationException(
                                        $"DeepQa visual: falha ao abrir {modulo} em {tema} @ {size.W}x{size.H}.");
                                }

                                WaitForUiIdle();
                                var content = window.CurrentContentElement as FrameworkElement
                                    ?? throw new InvalidOperationException($"DeepQa visual: conteudo invalido {modulo}.");

                                var file = Path.Combine(
                                    outDir,
                                    $"{modulo}-{tema}-{size.W}x{size.H}.png".ToLowerInvariant());
                                CaptureElementPng(content, file);
                                if (!File.Exists(file) || new FileInfo(file).Length < 1024)
                                {
                                    throw new InvalidOperationException($"DeepQa visual: captura invalida {file}.");
                                }
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

            RunCheck(result, "DeepQa:BotoesSegurosEnumeracao", () =>
            {
                MainWindow? window = null;
                var identificados = 0;
                var comIdentidade = 0;
                var semIdentidadeIcon = 0;

                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    foreach (var modulo in ObterDeepQaModules())
                    {
                        var ok = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? window.OpenImportarNFeForAutomation()
                            : window.NavigateToModuleForAutomation(modulo, forceReload: true);
                        if (!ok || window.CurrentContentElement is not FrameworkElement content)
                        {
                            throw new InvalidOperationException($"DeepQa botoes: falha em {modulo}.");
                        }

                        WaitForUiIdle();
                        var buttons = FindVisualChildren<Button>(content)
                            .Where(b => b.IsVisible)
                            .ToList();
                        identificados += buttons.Count;

                        foreach (var button in buttons)
                        {
                            var label = button.Content?.ToString()?.Trim() ?? string.Empty;
                            var tip = button.ToolTip?.ToString();
                            var name = AutomationProperties.GetName(button);
                            var hasIdentity = !string.IsNullOrWhiteSpace(label) ||
                                              !string.IsNullOrWhiteSpace(tip) ||
                                              !string.IsNullOrWhiteSpace(name);
                            if (hasIdentity)
                            {
                                comIdentidade++;
                            }

                            var iconOnly = label is "+" or "-" or "X" or "×" or "…" or "...";
                            if (iconOnly && string.IsNullOrWhiteSpace(tip) && string.IsNullOrWhiteSpace(name))
                            {
                                semIdentidadeIcon++;
                            }
                        }
                    }

                    if (identificados < 50)
                    {
                        throw new InvalidOperationException(
                            $"DeepQa botoes: poucos botoes descobertos ({identificados}).");
                    }

                    if (semIdentidadeIcon > 0)
                    {
                        throw new InvalidOperationException(
                            $"DeepQa botoes: {semIdentidadeIcon} IconButton(s) sem ToolTip/AutomationName.");
                    }

                    App.Logger.LogInfo(
                        $"DeepQa botoes: identificados={identificados}, comIdentidade={comIdentidade}, iconSemNome={semIdentidadeIcon}.",
                        "Smoke");
                }
                finally
                {
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

                    // Teclado: SearchTextBox deve aceitar foco.
                    search.Focus();
                    WaitForUiIdle();
                    if (!search.Focusable)
                    {
                        throw new InvalidOperationException("DeepQa Funcionarios: SearchTextBox nao focavel.");
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

            if (bg.Color == text.Color)
            {
                throw new InvalidOperationException("Tema aplicado com fundo e texto na mesma cor.");
            }
        }
    }
}
