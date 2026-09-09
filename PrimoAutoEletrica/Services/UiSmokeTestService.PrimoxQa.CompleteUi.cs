using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        /// <summary>
        /// Fase 15E — foco, Dark surfaces, layout, botão-a-botão (continua após falha).
        /// </summary>
        private void RunPrimoxCompleteUiChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            GarantirBancoIsoladoDoSmoke("PRIMOX Complete UI 15E");
            _fixture ??= EnsureSmokeFixture(syntheticUser);
            var modules = ObterDeepQaModules();
            var evidenceDir = Path.Combine(App.RuntimeAppDataPath, "Logs", "qa-visual", "fase15e");
            Directory.CreateDirectory(evidenceDir);
            var failures = new List<string>();
            var reportLines = new List<string>
            {
                "# PRIMOX Complete UI 15E — evidência runtime",
                $"GeneratedAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                ""
            };

            void NoteFail(string id, string detail)
            {
                failures.Add($"{id}: {detail}");
                reportLines.Add($"- FAIL {id}: {detail}");
                App.Logger.LogWarning($"CompleteUI fail {id}: {detail}", "Smoke");
            }

            RunCheck(result, "QaEngine:CompleteUiFocusVisualStyle", () =>
            {
                // Garante recurso global + leitura segura em foco/teclado (P15E-001/002).
                if (Application.Current.TryFindResource("PrimoxFocusVisual") == null)
                {
                    throw new InvalidOperationException("PrimoxFocusVisual ausente.");
                }

                if (Application.Current.TryFindResource(SystemParameters.FocusVisualStyleKey) == null)
                {
                    throw new InvalidOperationException("SystemParameters.FocusVisualStyleKey nao mapeado para Primox.");
                }

                MainWindow? window = null;
                LoginWindow? login = null;
                try
                {
                    login = new LoginWindow();
                    ShowWindowForInteraction(login);
                    WaitForUiIdle();

                    ProbeFocusVisualSafe(login, "LoginWindow", NoteFail);
                    SimulateKeyboardFocusWalk(login, NoteFail);

                    // Simula reentrada de foco (mover/ativar janela).
                    login.Activate();
                    WaitForUiIdle();
                    ProbeFocusVisualSafe(login, "LoginWindow-Activate", NoteFail);

                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    WaitForUiIdle();

                    foreach (var modulo in modules)
                    {
                        var ok = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? window.OpenImportarNFeForAutomation()
                            : window.NavigateToModuleForAutomation(modulo, forceReload: true);
                        if (!ok || window.CurrentContentElement is not FrameworkElement content)
                        {
                            NoteFail("NAV", $"Falha ao abrir {modulo}");
                            continue;
                        }

                        WaitForUiIdle();
                        ProbeFocusVisualSafe(content, modulo, NoteFail);
                        SimulateKeyboardFocusWalk(content, NoteFail);
                    }

                    if (failures.Any(f => f.Contains("FocusVisual", StringComparison.OrdinalIgnoreCase)
                                          || f.Contains("UnsetValue", StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException(
                            "FocusVisualStyle ainda falha: " + string.Join(" | ", failures.Take(5)));
                    }
                }
                finally
                {
                    if (login?.IsVisible == true) login.Close();
                    if (window?.IsVisible == true) window.Close();
                }
            });

            RunCheck(result, "QaEngine:CompleteUiDarkInputSurfaces", () =>
            {
                var theme = new ThemeService();
                var original = theme.GetCurrentTheme();
                MainWindow? window = null;
                try
                {
                    theme.ApplyTheme(AppTheme.Dark);
                    WaitForUiIdle();

                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    var whiteHits = new List<string>();
                    var targets = new[]
                    {
                        "Clientes", "Veiculos", "Estoque", "CatalogoPecas", "ImportarNFe",
                        "AutoEletricaTecnica", "Funcionarios", "PDV"
                    };

                    foreach (var modulo in targets)
                    {
                        var ok = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? window.OpenImportarNFeForAutomation()
                            : window.NavigateToModuleForAutomation(modulo, forceReload: true);
                        if (!ok || window.CurrentContentElement is not FrameworkElement content)
                        {
                            continue;
                        }

                        WaitForUiIdle();
                        foreach (var tb in FindVisualChildren<TextBox>(content))
                        {
                            if (IsNearWhiteBrush(tb.Background))
                            {
                                whiteHits.Add($"{modulo}/{tb.Name ?? "TextBox"}");
                            }
                        }

                        foreach (var dp in FindVisualChildren<DatePicker>(content))
                        {
                            if (IsNearWhiteBrush(dp.Background))
                            {
                                whiteHits.Add($"{modulo}/{dp.Name ?? "DatePicker"}");
                            }
                        }
                    }

                    var produtos = App.Repositories.Produtos.ObterTodos().Take(20).ToList();
                    if (produtos.Count == 0 && _fixture?.Produto != null)
                    {
                        produtos.Add(_fixture.Produto);
                    }

                    var pdvWin = new SelecionarProdutoPDVWindow(produtos);
                    try
                    {
                        ShowWindowForInteraction(pdvWin);
                        WaitForUiIdle();
                        foreach (var tb in FindVisualChildren<TextBox>(pdvWin))
                        {
                            if (IsNearWhiteBrush(tb.Background))
                            {
                                whiteHits.Add($"SelecionarProdutoPDV/{tb.Name ?? "TextBox"}");
                            }
                        }

                        if (pdvWin.ActualWidth > SystemParameters.WorkArea.Width + 20)
                        {
                            NoteFail("P15E-004", $"Largura PDV produtos {pdvWin.ActualWidth:0} > WorkArea");
                        }
                    }
                    finally
                    {
                        if (pdvWin.IsVisible) pdvWin.Close();
                    }

                    if (whiteHits.Count > 0)
                    {
                        throw new InvalidOperationException(
                            "Inputs com fundo branco indevido no Dark: " + string.Join(", ", whiteHits.Take(12)));
                    }
                }
                finally
                {
                    if (window?.IsVisible == true) window.Close();
                    theme.ApplyTheme(original);
                    WaitForUiIdle();
                }
            });

            RunCheck(result, "QaEngine:CompleteUiButtonByButton", () =>
            {
                MainWindow? window = null;
                var tested = 0;
                var pass = 0;
                var fail = 0;
                var skipped = 0;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    foreach (var modulo in modules)
                    {
                        var ok = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? window.OpenImportarNFeForAutomation()
                            : window.NavigateToModuleForAutomation(modulo, forceReload: true);
                        if (!ok || window.CurrentContentElement is not FrameworkElement content)
                        {
                            fail++;
                            NoteFail("BTN-NAV", modulo);
                            continue;
                        }

                        WaitForUiIdle();
                        var buttons = PrimoxQaEngine.FindButtons(content)
                            .Where(b => b.IsVisible && b.IsEnabled)
                            .Take(10)
                            .ToList();

                        foreach (var button in buttons)
                        {
                            tested++;
                            var label = ExtractButtonText(button);
                            if (IsDestructiveOrUnsafeButton(label, button) || IsDialogHeavyButton(label, button))
                            {
                                skipped++;
                                reportLines.Add($"- SKIP {modulo}/{label}");
                                continue;
                            }

                            try
                            {
                                ProbeFocusVisualSafe(button, $"{modulo}/{label}", NoteFail);
                                // Prefer Focus+keyboard over RaiseEvent Click (menos diálogos nativos).
                                button.Focus();
                                _ = button.FocusVisualStyle;
                                pass++;
                                reportLines.Add($"- PASS-FOCUS {modulo}/{label}");
                            }
                            catch (Exception ex)
                            {
                                fail++;
                                NoteFail("BTN", $"{modulo}/{label}: {ex.Message}");
                            }
                        }

                        // Um clique seguro por módulo (botão secundário/ghost sem abrir file dialog).
                        var safeClick = buttons.FirstOrDefault(b =>
                        {
                            var l = ExtractButtonText(b);
                            return !IsDestructiveOrUnsafeButton(l, b)
                                   && !IsDialogHeavyButton(l, b)
                                   && (l.Contains("Atualizar", StringComparison.OrdinalIgnoreCase)
                                       || l.Contains("Limpar", StringComparison.OrdinalIgnoreCase)
                                       || l.Contains("Refresh", StringComparison.OrdinalIgnoreCase)
                                       || string.Equals(b.Name, "AtualizarTecnicaButton", StringComparison.OrdinalIgnoreCase));
                        });
                        if (safeClick != null)
                        {
                            try
                            {
                                safeClick.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                                WaitForUiIdle();
                                CloseOwnedWindowsExcept(window, NoteFail);
                                pass++;
                                reportLines.Add($"- PASS-CLICK {modulo}/{ExtractButtonText(safeClick)}");
                            }
                            catch (Exception ex)
                            {
                                fail++;
                                NoteFail("BTN-CLICK", $"{modulo}: {ex.Message}");
                                CloseOwnedWindowsExcept(window, NoteFail);
                            }
                        }
                    }

                    reportLines.Add("");
                    reportLines.Add($"Tested={tested} Pass={pass} Fail={fail} Skipped={skipped}");
                    File.WriteAllText(
                        Path.Combine(evidenceDir, $"complete-ui-buttons-{DateTime.Now:yyyyMMdd-HHmmss}.md"),
                        string.Join(Environment.NewLine, reportLines),
                        Encoding.UTF8);

                    if (tested < 40)
                    {
                        throw new InvalidOperationException($"Poucos botoes testados ({tested}).");
                    }

                    if (fail > 0)
                    {
                        throw new InvalidOperationException(
                            $"Button-by-button: {fail} falhas em {tested} (pass={pass}, skip={skipped}). " +
                            string.Join(" | ", failures.Take(8)));
                    }
                }
                finally
                {
                    if (window?.IsVisible == true) window.Close();
                }
            });

            RunCheck(result, "QaEngine:CompleteUiPlaceholdersI18n", () =>
            {
                var helper = LocalizationHelper.Instance;
                var cultures = new[] { "pt-BR", "en-US", "es-ES" };
                foreach (var culture in cultures)
                {
                    helper.SetLanguage(culture);
                    var ph = helper.SearchPlaceholder;
                    if (string.IsNullOrWhiteSpace(ph) || !ph.Contains("...", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"SearchPlaceholder invalido para {culture}: '{ph}'.");
                    }
                }

                helper.SetLanguage("pt-BR");
            });

            RunCheck(result, "QaEngine:CompleteUiLayoutActions", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    if (!window.NavigateToModuleForAutomation("Funcionarios", forceReload: true) ||
                        window.CurrentContentElement is not FrameworkElement content)
                    {
                        throw new InvalidOperationException("Funcionarios nao abriu.");
                    }

                    WaitForUiIdle();
                    var grid = FindVisualChildren<DataGrid>(content).FirstOrDefault()
                        ?? throw new InvalidOperationException("DataGrid funcionarios ausente.");
                    var acoes = grid.Columns.FirstOrDefault(c =>
                        (c.Header?.ToString() ?? string.Empty).Contains("Aco", StringComparison.OrdinalIgnoreCase));
                    if (acoes == null)
                    {
                        throw new InvalidOperationException("Coluna Acoes ausente.");
                    }

                    if (acoes.ActualWidth > 0 && acoes.ActualWidth < 200)
                    {
                        throw new InvalidOperationException(
                            $"Coluna Acoes estreita demais: {acoes.ActualWidth:0}px (P15E-014).");
                    }

                    // Login close button geometry
                    var login = new LoginWindow();
                    try
                    {
                        ShowWindowForInteraction(login);
                        WaitForUiIdle();
                        var close = FindElementByName<Button>(login, "CloseButton")
                            ?? throw new InvalidOperationException("CloseButton ausente no Login.");
                        if (close.ActualWidth < 28 || close.ActualHeight < 28)
                        {
                            throw new InvalidOperationException(
                                $"CloseButton pequeno demais: {close.ActualWidth:0}x{close.ActualHeight:0}.");
                        }

                        if (close.ActualWidth > login.ActualWidth)
                        {
                            throw new InvalidOperationException("CloseButton fora da janela.");
                        }
                    }
                    finally
                    {
                        if (login.IsVisible) login.Close();
                    }
                }
                finally
                {
                    if (window?.IsVisible == true) window.Close();
                }
            });

            RunCheck(result, "QaEngine:CompleteUiHelpCenter", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    if (!window.NavigateToModuleForAutomation("Help", forceReload: true) &&
                        !window.NavigateToModuleForAutomation("Ajuda", forceReload: true))
                    {
                        throw new InvalidOperationException("Modulo Ajuda/Help nao abriu.");
                    }

                    WaitForUiIdle();
                    if (window.CurrentContentElement is not PrimoAutoEletrica.UserControls.HelpControl help)
                    {
                        throw new InvalidOperationException(
                            $"Conteudo da Ajuda nao e HelpControl (atual={window.CurrentContentElement?.GetType().Name}).");
                    }

                    var search = FindElementByName<TextBox>(help, "SearchBox")
                        ?? throw new InvalidOperationException("SearchBox da Ajuda ausente.");
                    search.Text = "cliente";
                    WaitForUiIdle();

                    help.NavigateToTopic("criar-cliente");
                    WaitForUiIdle();
                    var contentArea = FindElementByName<StackPanel>(help, "ContentArea")
                        ?? throw new InvalidOperationException("ContentArea da Ajuda ausente.");
                    if (contentArea.Children.Count == 0)
                    {
                        throw new InvalidOperationException("Ajuda nao renderizou topico criar-cliente.");
                    }

                    help.NavigateToTopic("limites-produto");
                    WaitForUiIdle();
                    if (contentArea.Children.Count == 0)
                    {
                        throw new InvalidOperationException("Ajuda nao renderizou topico limites-produto.");
                    }

                    if (help.Background == null)
                    {
                        throw new InvalidOperationException("HelpControl sem Background.");
                    }

                    // Help 3.0: índice secundário (<=260), não segunda Sidebar (>=330).
                    if (help.Content is Grid rootGrid &&
                        rootGrid.ColumnDefinitions.Count >= 1)
                    {
                        var indexWidth = rootGrid.ColumnDefinitions[0].Width;
                        if (indexWidth.IsAbsolute && indexWidth.Value > 260)
                        {
                            throw new InvalidOperationException(
                                $"Indice da Ajuda demasiado largo ({indexWidth.Value}px) — parece Sidebar secundaria.");
                        }

                        var maxW = rootGrid.ColumnDefinitions[0].MaxWidth;
                        if (!double.IsNaN(maxW) && maxW > 280)
                        {
                            throw new InvalidOperationException(
                                $"MaxWidth do indice da Ajuda excessivo ({maxW}).");
                        }
                    }

                    // Percorre secoes chave (navegaçao + conteudo).
                    foreach (var tag in new[]
                             {
                                 "comece-aqui", "glossario", "cargo-caixa", "dia-trabalho",
                                 "criar-os", "modulo-pdv", "limites-produto", "faq"
                             })
                    {
                        help.NavigateToTopic(tag);
                        WaitForUiIdle();
                        if (contentArea.Children.Count == 0)
                        {
                            throw new InvalidOperationException($"Ajuda nao renderizou topico {tag}.");
                        }
                    }
                }
                finally
                {
                    if (window?.IsVisible == true) window.Close();
                }
            });
        }

        private static bool IsNearWhiteBrush(Brush? brush)
        {
            if (brush is SolidColorBrush solid)
            {
                var c = solid.Color;
                return c.A > 200 && c.R >= 245 && c.G >= 245 && c.B >= 245;
            }

            return false;
        }

        private static void ProbeFocusVisualSafe(
            DependencyObject root,
            string context,
            Action<string, string> noteFail)
        {
            FocusVisualStyleHealer.HealSubtree(root);
            AccessibilityChromeHealer.HealSubtree(root);

            foreach (var fe in FindVisualChildren<FrameworkElement>(root)
                         .Where(e => e.Focusable && e.IsVisible)
                         .Take(40))
            {
                try
                {
                    FocusVisualStyleHealer.HealElement(fe, FocusVisualStyleHealer.ResolveSafeStyle(fe));
                    _ = fe.FocusVisualStyle;
                }
                catch (Exception ex)
                {
                    noteFail("FocusVisual", $"{context}/{fe.GetType().Name}/{fe.Name}: {ex.Message}");
                }
            }

            if (root is FrameworkElement rootFe)
            {
                try
                {
                    FocusVisualStyleHealer.HealElement(rootFe, FocusVisualStyleHealer.ResolveSafeStyle(rootFe));
                    _ = rootFe.FocusVisualStyle;
                }
                catch (Exception ex)
                {
                    noteFail("FocusVisual", $"{context}/root: {ex.Message}");
                }
            }
        }

        private static void SimulateKeyboardFocusWalk(
            DependencyObject root,
            Action<string, string> noteFail)
        {
            var focusables = FindVisualChildren<FrameworkElement>(root)
                .Where(e => e.Focusable && e.IsVisible && e.IsEnabled)
                .Take(25)
                .ToList();

            foreach (var fe in focusables)
            {
                try
                {
                    fe.Focus();
                    Keyboard.Focus(fe);
                    _ = fe.FocusVisualStyle;
                }
                catch (Exception ex)
                {
                    noteFail("FocusWalk", $"{fe.GetType().Name}/{fe.Name}: {ex.Message}");
                }
            }
        }

        private static bool IsDialogHeavyButton(string label, Button button)
        {
            var text = (label + " " + (button.ToolTip?.ToString() ?? string.Empty) + " " + (button.Name ?? string.Empty))
                .ToLowerInvariant();
            string[] blocked =
            {
                "novo", "new", "editar", "edit", "abrir", "open", "import", "export", "imprim", "print",
                "anexo", "foto", "arquivo", "browse", "selecionar", "gerar orc", "orcamento", "pdf",
                "excel", "csv", "whatsapp", "email", "nfe", "xml"
            };
            return blocked.Any(b => text.Contains(b, StringComparison.Ordinal));
        }

        private void CloseOwnedWindowsExcept(Window main, Action<string, string> noteFail)
        {
            foreach (Window owned in Application.Current.Windows.OfType<Window>()
                         .Where(w => !ReferenceEquals(w, main) && w.IsVisible)
                         .ToList())
            {
                try
                {
                    owned.Close();
                    WaitForUiIdle();
                }
                catch (Exception ex)
                {
                    noteFail("CLOSE", $"{owned.GetType().Name}: {ex.Message}");
                }
            }
        }

        private static bool IsDestructiveOrUnsafeButton(string label, Button button)
        {
            var text = (label + " " + (button.ToolTip?.ToString() ?? string.Empty) + " " + (button.Name ?? string.Empty))
                .ToLowerInvariant();
            string[] blocked =
            {
                "excluir", "delete", "inativar", "remover", "reset", "apagar", "formatar",
                "backup", "restore", "restaurar", "desinstalar", "logout", "sair",
                "emitir nfe", "transmitir", "pagar", "estornar"
            };
            return blocked.Any(b => text.Contains(b, StringComparison.Ordinal));
        }

        private static bool LooksLikeCloseOrCancel(string label)
        {
            var t = label.ToLowerInvariant();
            return t.Contains("cancel") || t.Contains("fechar") || t.Contains("close")
                   || t.Contains("voltar") || t == "x";
        }
    }
}
