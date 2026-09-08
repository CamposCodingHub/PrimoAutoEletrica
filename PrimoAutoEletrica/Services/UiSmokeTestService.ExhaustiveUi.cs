using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// PRIMOX Exhaustive Button Simulation — varredura EXCLUSIVA de botões
    /// (sem calendário/células/campos), com recursão em janelas e pop-ups.
    /// Filtros: ExhaustiveUi | ExhaustiveButtonSimulation
    /// </summary>
    public sealed partial class UiSmokeTestService
    {
        private const int ExhaustiveMaxWindowDepth = 5;
        private const int ExhaustiveMaxSameSignature = 2;
        private const int ExhaustiveActionTimeoutMs = 12000;

        private void RunExhaustiveUiChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            GarantirBancoIsoladoDoSmoke("PRIMOX Exhaustive UI");
            _fixture ??= EnsureSmokeFixture(syntheticUser);

            var evidenceRoot = Path.Combine(
                App.RuntimeAppDataPath,
                "Logs",
                "qa-visual",
                "exhaustive-ui",
                DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(evidenceRoot);
            var repoEvidence = Path.Combine(
                Path.GetDirectoryName(typeof(App).Assembly.Location) ?? ".",
                "..", "..", "..", "..", "TestResults", "UiSmoke", "ExhaustiveUi");
            try
            {
                repoEvidence = Path.GetFullPath(repoEvidence);
                Directory.CreateDirectory(repoEvidence);
            }
            catch
            {
                repoEvidence = Path.Combine(App.RuntimeAppDataPath, "Logs", "ExhaustiveUi");
                Directory.CreateDirectory(repoEvidence);
            }

            RunCheck(result, "ExhaustiveUi:FullSimulation", () =>
            {
                var modules = ObterDeepQaModules();
                var themeService = new ThemeService();
                var originalTheme = themeService.GetCurrentTheme();

                // Rounds executados — registrar explicitamente (não reduzir em silêncio).
                var rounds = new List<(AppTheme Theme, int Width, int Height, string Label)>
                {
                    (AppTheme.Light, 1366, 768, "Light-1366x768"),
                    (AppTheme.Dark, 1366, 768, "Dark-1366x768"),
                    (AppTheme.Light, 1600, 900, "Light-1600x900"),
                    (AppTheme.Dark, 1600, 900, "Dark-1600x900"),
                    (AppTheme.Light, 1920, 1080, "Light-1920x1080"),
                    (AppTheme.Dark, 1920, 1080, "Dark-1920x1080"),
                    (AppTheme.Light, 2560, 1440, "Light-2560x1440"),
                    (AppTheme.Dark, 2560, 1440, "Dark-2560x1440"),
                };

                var global = new ExhaustiveRunState(evidenceRoot, repoEvidence);
                global.RoundsPlanned.AddRange(rounds.Select(r => r.Label));

                try
                {
                    foreach (var round in rounds)
                    {
                        try
                        {
                            themeService.ApplyTheme(round.Theme);
                            WaitForUiIdle();
                            var roundState = RunExhaustiveRound(
                                syntheticUser,
                                modules,
                                round.Theme,
                                round.Width,
                                round.Height,
                                round.Label,
                                global);
                            global.Merge(roundState);
                            global.RoundsExecuted.Add(round.Label);
                            App.Logger.LogInfo(
                                $"ExhaustiveUi round {round.Label}: discovered={roundState.Discovered} tested={roundState.Tested} pass={roundState.Pass} fail={roundState.Fail}",
                                "Smoke");
                        }
                        catch (Exception exRound)
                        {
                            global.RecordRoundCrash(round.Label, exRound);
                            App.Logger.LogError($"ExhaustiveUi round {round.Label} crashed", exRound);
                            try { CloseAllOwnedExcept(null); } catch { /* continue */ }
                        }
                    }
                }
                finally
                {
                    themeService.ApplyTheme(originalTheme);
                    WaitForUiIdle();
                    CloseAllOwnedExcept(null);
                }

                global.WriteArtifacts();

                if (global.Discovered == 0)
                {
                    throw new InvalidOperationException(
                        "ExhaustiveUi: zero botoes descobertos. " +
                        string.Join(" | ", global.Failures.Take(8)));
                }

                if (global.Fail > 0)
                {
                    throw new InvalidOperationException(
                        $"ExhaustiveUi: {global.Fail} FAIL / {global.Tested} tested / {global.Discovered} discovered. " +
                        string.Join(" | ", global.Failures.Take(12)));
                }

                App.Logger.LogInfo(
                    $"ExhaustiveUi OK: discovered={global.Discovered} tested={global.Tested} pass={global.Pass} " +
                    $"fail={global.Fail} skipped={global.Skipped} blocked={global.Blocked} coverage={global.ExecutionCoveragePct:F1}%",
                    "Smoke");
            });
        }

        private ExhaustiveRoundState RunExhaustiveRound(
            Funcionario user,
            IReadOnlyList<string> modules,
            AppTheme theme,
            int width,
            int height,
            string roundLabel,
            ExhaustiveRunState global)
        {
            var state = new ExhaustiveRoundState(roundLabel, theme, width, height);
            MainWindow? main = null;
            ExhaustivePopupGuardian? guardian = null;
            try
            {
                // Login surface (janela não-página)
                var login = new LoginWindow();
                guardian = new ExhaustivePopupGuardian(state, () => login);
                state.CurrentModule = "Login";
                guardian.SetModalExplorer(w => ExploreSurface(
                    w,
                    main: null,
                    module: state.CurrentModule,
                    depth: 1,
                    breadcrumb: $"{state.CurrentModule}/{w.GetType().Name}",
                    state: state,
                    signatureCounts: state.SignatureCounts,
                    guardian: guardian));
                guardian.Start();
                try
                {
                    ShowWindowForInteraction(login);
                    ApplyWindowSize(login, Math.Min(width, 900), Math.Min(height, 700));
                    WaitForUiIdle();
                    DrainPopups(guardian, state, "Login:pre");
                    FocusVisualStyleHealer.HealSubtree(login);
                    ExploreSurface(
                        login,
                        main: null,
                        module: "Login",
                        depth: 0,
                        breadcrumb: "LoginWindow",
                        state: state,
                        signatureCounts: state.SignatureCounts,
                        guardian: guardian);
                    DrainPopups(guardian, state, "Login:post");
                }
                finally
                {
                    if (login.IsVisible)
                    {
                        login.Close();
                    }

                    WaitForUiIdle();
                    DrainPopups(guardian, state, "Login:close");
                }

                guardian.Dispose();
                main = new MainWindow(user);
                guardian = new ExhaustivePopupGuardian(state, () => main);
                guardian.SetModalExplorer(w => ExploreSurface(
                    w,
                    main,
                    module: state.CurrentModule,
                    depth: 1,
                    breadcrumb: $"{state.CurrentModule}/{w.GetType().Name}",
                    state: state,
                    signatureCounts: state.SignatureCounts,
                    guardian: guardian));
                guardian.Start();
                ShowWindowForInteraction(main);
                ApplyWindowSize(main, width, height);
                WaitForUiIdle();
                DrainPopups(guardian, state, "Main:pre");
                FocusVisualStyleHealer.HealSubtree(main);

                foreach (var modulo in modules)
                {
                    try
                    {
                        state.CurrentModule = modulo;
                        var ok = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? main.OpenImportarNFeForAutomation()
                            : main.NavigateToModuleForAutomation(modulo, forceReload: true);
                        if (!ok || main.CurrentContentElement is not FrameworkElement content)
                        {
                            state.AddFailure($"NAV:{modulo}", "Falha ao abrir modulo");
                            DrainPopups(guardian, state, $"NAV-fail:{modulo}");
                            continue;
                        }

                        WaitForUiIdle(4);
                        DrainPopups(guardian, state, $"NAV:{modulo}");
                        FocusVisualStyleHealer.HealSubtree(content);
                        state.PagesVisited.Add(modulo);
                        state.Tree.AppendLine($"{modulo}");

                        ExploreSurface(
                            content,
                            main,
                            modulo,
                            depth: 0,
                            breadcrumb: modulo,
                            state: state,
                            signatureCounts: state.SignatureCounts,
                            guardian: guardian);

                        CloseOwnedExcept(main);
                        DrainPopups(guardian, state, $"MODULE-end:{modulo}");
                    }
                    catch (Exception ex)
                    {
                        state.AddFailure($"MODULE:{modulo}", ex.Message);
                        DrainPopups(guardian, state, $"MODULE-ex:{modulo}");
                        try { CloseOwnedExcept(main); } catch { /* continue */ }
                    }
                }
            }
            finally
            {
                try { guardian?.Dispose(); } catch { /* ignore */ }
                if (main?.IsVisible == true)
                {
                    main.Close();
                }

                WaitForUiIdle();
            }

            return state;
        }

        private void ExploreSurface(
            DependencyObject root,
            MainWindow? main,
            string module,
            int depth,
            string breadcrumb,
            ExhaustiveRoundState state,
            Dictionary<string, int> signatureCounts,
            ExhaustivePopupGuardian? guardian)
        {
            if (depth > ExhaustiveMaxWindowDepth)
            {
                state.AddResult(new ExhaustiveButtonResult
                {
                    Status = "BLOCKED",
                    Module = module,
                    Window = breadcrumb,
                    Content = "(depth-limit)",
                    Detail = $"MaximumWindowDepth={ExhaustiveMaxWindowDepth}"
                });
                return;
            }

            guardian?.ProtectWindow(Window.GetWindow(root as DependencyObject) ?? root as Window);

            var window = Window.GetWindow(root as DependencyObject) ?? root as Window;
            var windowName = window?.GetType().Name ?? root.GetType().Name;
            state.WindowsVisited.Add($"{module}/{windowName}");

            FocusVisualStyleHealer.HealSubtree(root as DependencyObject ?? window);
            AccessibilityChromeHealer.HealSubtree(root as DependencyObject ?? window);

            // 1) SCAN completo — congelar fila ANTES de qualquer clique.
            // Somente botões reais (sem CalendarDayButton / scrollbar / CheckBox / RadioButton).
            var frozenQueue = DiscoverAllButtons(root);
            state.Discovered += frozenQueue.Count;

            var indent = new string(' ', Math.Min(depth, 8) * 2);
            state.Tree.AppendLine($"{indent}### SCAN {breadcrumb} ({windowName}) — {frozenQueue.Count} botões mapeados");
            var mapped = new List<(ButtonBase Button, string ButtonId, string Label, string Name, string Automation, string Tip)>();
            foreach (var button in frozenQueue
                         .OrderBy(b => GetExhaustiveExecutionPriority(b, window, depth)))
            {
                var buttonId = $"BTN-{state.NextButtonId():000000}";
                string label;
                string automation;
                string tip;
                string name;
                try
                {
                    label = ExtractButtonBaseText(button);
                    automation = AutomationProperties.GetName(button) ?? string.Empty;
                    tip = button.ToolTip?.ToString() ?? string.Empty;
                    name = string.IsNullOrWhiteSpace(button.Name) ? "(sem Name)" : button.Name;
                }
                catch
                {
                    label = "(unreadable)";
                    automation = "";
                    tip = "";
                    name = "(sem Name)";
                }

                mapped.Add((button, buttonId, label, name, automation, tip));
                state.Tree.AppendLine(
                    $"{indent}  {buttonId} [{button.GetType().Name}] '{label}' name={name} enabled={button.IsEnabled} visible={button.IsVisible}");
            }

            // Campos observados — NÃO entram na fila de clique de botões.
            InventoryFields(root, module, windowName, state);

            // 2) EXECUTAR fila congelada.
            for (var qi = 0; qi < mapped.Count; qi++)
            {
                var item = mapped[qi];
                var button = item.Button;
                var buttonId = item.ButtonId;
                var label = item.Label;
                var automation = item.Automation;
                var tip = item.Tip;
                var name = item.Name;

                if (!IsUiAlive(root, window))
                {
                    // Flush honesto: todos os restantes — não só o primeiro.
                    FlushUnreachableQueue(
                        mapped,
                        qi,
                        module,
                        breadcrumb,
                        state,
                        "NOT_TESTABLE",
                        "HOST_CLOSED_BY_PRIOR_ACTION");
                    break;
                }

                try
                {
                    _ = button.IsVisible;
                }
                catch
                {
                    state.Skipped++;
                    state.AddResult(new ExhaustiveButtonResult
                    {
                        Status = "NOT_TESTABLE",
                        Module = module,
                        Window = breadcrumb,
                        ControlType = button.GetType().Name,
                        ControlName = name,
                        Content = label,
                        Detail = $"{buttonId} BUTTON_REF_INVALID_AFTER_PRIOR_ACTION"
                    });
                    continue;
                }

                var signature = $"{windowName}|{button.GetType().Name}|{name}|{label}|{depth}";

                if (!signatureCounts.TryGetValue(signature, out var visits))
                {
                    visits = 0;
                }

                visits++;
                signatureCounts[signature] = visits;
                if (visits > ExhaustiveMaxSameSignature)
                {
                    state.Skipped++;
                    state.AddResult(new ExhaustiveButtonResult
                    {
                        Status = "SKIPPED",
                        Module = module,
                        Window = breadcrumb,
                        ControlName = name,
                        Content = label,
                        Detail = $"{buttonId} NAVIGATION_LOOP_PROTECTED / MaxSameWindowReentry"
                    });
                    continue;
                }

                state.Tree.AppendLine($"{indent}├── EXEC {buttonId} [{button.GetType().Name}] {label} ({name})");

                var row = new ExhaustiveButtonResult
                {
                    Theme = state.Theme.ToString(),
                    Resolution = $"{state.Width}x{state.Height}",
                    Module = module,
                    Window = breadcrumb,
                    ControlType = button.GetType().Name,
                    ControlName = name,
                    Content = label,
                    AutomationName = automation,
                    ToolTip = tip,
                    Enabled = button.IsEnabled,
                    Visible = button.IsVisible,
                    Detail = buttonId + ";"
                };

                // Layout / geometry
                try
                {
                    if (button.IsVisible && window is Visual windowVisual
                        && button is Visual buttonVisual
                        && windowVisual != buttonVisual
                        && button.IsLoaded
                        && window.IsLoaded)
                    {
                        var current = buttonVisual as DependencyObject;
                        var isDescendant = false;
                        while (current != null)
                        {
                            if (ReferenceEquals(current, windowVisual))
                            {
                                isDescendant = true;
                                break;
                            }

                            current = VisualTreeHelper.GetParent(current);
                        }

                        if (isDescendant)
                        {
                            var transform = button.TransformToAncestor(windowVisual);
                            var topLeft = transform.Transform(new Point(0, 0));
                            if (topLeft.X < -2 || topLeft.Y < -2
                                || topLeft.X > window.ActualWidth + 2
                                || topLeft.Y > window.ActualHeight + 2)
                            {
                                row.Detail += " outside-window;";
                            }
                        }

                        if (button.ActualWidth > 0 && button.ActualWidth < 8)
                        {
                            row.Detail += " clipped-width;";
                        }
                    }
                }
                catch
                {
                    // geometry optional
                }

                if (!button.IsVisible)
                {
                    row.Status = "NOT_VISIBLE";
                    state.Skipped++;
                    state.AddResult(row);
                    continue;
                }

                if (!button.IsEnabled)
                {
                    row.Status = "EXPECTED_DISABLED";
                    state.Skipped++;
                    state.AddResult(row);
                    continue;
                }

                // Acessibilidade: ícone/chrome sem tip/nome (após heal de SelectAll/DatePicker)
                AccessibilityChromeHealer.HealButton(button);
                tip = button.ToolTip?.ToString() ?? tip;
                automation = AutomationProperties.GetName(button) ?? automation;
                if (string.IsNullOrWhiteSpace(label)
                    && string.IsNullOrWhiteSpace(tip)
                    && string.IsNullOrWhiteSpace(automation)
                    && string.IsNullOrWhiteSpace(button.Name))
                {
                    row.Detail += " ACCESSIBILITY ISSUE / UNIDENTIFIED_BUTTON;";
                }

                var isDestructive = IsExhaustiveDestructive(label, tip, name);
                var beforeWindows = SnapshotWindows();
                guardian?.ClearLastErrorPopup();

                // Botões que abrem seletor de arquivo/impressão — não bloquear a UI thread.
                    if (IsFilePickerOrPrintTrigger(label, tip, name))
                    {
                        row.Status = "NOT_TESTABLE";
                        row.Detail += " NATIVE_DIALOG file-picker-or-print;";
                        state.Skipped++;
                        state.AddResult(row);
                        continue;
                    }

                // Nunca executar Shutdown do Login (CloseButton_Click → Application.Shutdown).
                if (window is LoginWindow
                    && (string.Equals(name, "CloseButton", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(label, "CloseButton", StringComparison.OrdinalIgnoreCase)))
                {
                    row.Status = "NOT_TESTABLE";
                    row.Detail += " LOGIN_CLOSE_SKIPPED_PRESERVE_SURFACE;";
                    state.Skipped++;
                    state.AddResult(row);
                    continue;
                }

                try
                {
                    var sw = Stopwatch.StartNew();
                    FocusVisualStyleHealer.HealElement(button, FocusVisualStyleHealer.ResolveSafeStyle(button));
                    button.Focus();
                    Keyboard.Focus(button);
                    _ = button.FocusVisualStyle;

                    if (IsWindowClosingControl(button, window, depth))
                    {
                        // Login Close destruiria a superfície antes do round MainWindow.
                        if (window is LoginWindow)
                        {
                            row.Status = "NOT_TESTABLE";
                            row.Detail += " LOGIN_CLOSE_SKIPPED_PRESERVE_SURFACE;";
                            state.Skipped++;
                            state.AddResult(row);
                            continue;
                        }

                        // Testa Fechar/X/Cancelar por último; não continua inventário após destruir a superfície.
                        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        DrainPopups(guardian, state, $"{module}/{buttonId}:close");
                        WaitForUiIdle(3);
                        row.Status = "PASS";
                        row.Detail += " window-close-tested;";
                        row.Clicked = true;
                        row.DurationMs = sw.ElapsedMilliseconds;
                        state.Pass++;
                        state.Tested++;
                        state.AddResult(row);
                        if (qi + 1 < mapped.Count)
                        {
                            FlushUnreachableQueue(
                                mapped,
                                qi + 1,
                                module,
                                breadcrumb,
                                state,
                                "NOT_TESTABLE",
                                "INTENTIONALLY_AFTER_WINDOW_CLOSE");
                        }
                        break;
                    }

                    if (isDestructive)
                    {
                        // Clique seguro: abre diálogo se houver e cancela — não confirma exclusão.
                        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        DrainPopups(guardian, state, $"{module}/{buttonId}:destructive");
                        WaitForUiIdle(3);
                        var opened = DiffWindows(beforeWindows);
                        foreach (var child in opened)
                        {
                            state.Tree.AppendLine($"{indent}│   └── {child.GetType().Name} (destructive-confirm)");
                            // Prefer cancel/close
                            ClickCancelOrClose(child);
                            if (child.IsVisible)
                            {
                                try { child.Close(); } catch { /* ignore */ }
                            }
                        }

                        row.Status = ClassifyAfterPopup(guardian, row, "destructive-safe-cancel;");
                        if (row.Status == "FAIL")
                        {
                            state.Fail++;
                            state.Failures.Add($"{buttonId} {module}/{label}: {row.Detail}");
                        }
                        else
                        {
                            state.Pass++;
                        }

                        row.Clicked = true;
                        row.DurationMs = sw.ElapsedMilliseconds;
                        state.Tested++;
                        state.AddResult(row);
                        continue;
                    }

                    // Login: preencher credenciais do fixture antes de Entrar (banco isolado do smoke).
                    if (window is LoginWindow)
                    {
                        TryFillLoginCredentials(window);
                    }

                    button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    DrainPopups(guardian, state, $"{module}/{buttonId}:click");
                    WaitForUiIdle(4);
                    sw.Stop();
                    row.Clicked = true;
                    row.DurationMs = sw.ElapsedMilliseconds;
                    if (row.DurationMs > 5000)
                    {
                        row.Detail += $" slow={row.DurationMs}ms;";
                    }

                    var children = DiffWindows(beforeWindows);
                    if (window is LoginWindow && IsLoginSubmitControl(button, label))
                    {
                        // MainWindow é explorada no round próprio — só valida Entrar e fecha extras.
                        foreach (var child in children)
                        {
                            row.OpenedWindow = child.GetType().Name;
                            try { if (child.IsVisible) child.Close(); } catch { /* ignore */ }
                        }

                        WaitForUiIdle(2);
                        row.Status = ClassifyAfterPopup(guardian, row, " login-submit-no-recurse;");
                        if (row.Status == "FAIL")
                        {
                            state.Fail++;
                            state.Failures.Add($"{buttonId} {module}/{label}: {row.Detail}");
                        }
                        else
                        {
                            state.Pass++;
                        }

                        state.Tested++;
                        state.AddResult(row);
                        continue;
                    }

                    if (children.Count > 0)
                    {
                        row.OpenedWindow = string.Join(",", children.Select(c => c.GetType().Name));
                        foreach (var child in children)
                        {
                            state.Tree.AppendLine($"{indent}│   └── {child.GetType().Name}");
                            try
                            {
                                FocusVisualStyleHealer.HealSubtree(child);
                                ExploreSurface(
                                    child,
                                    main,
                                    module,
                                    depth + 1,
                                    $"{breadcrumb}/{child.GetType().Name}",
                                    state,
                                    signatureCounts,
                                    guardian);
                            }
                            catch (Exception exChild)
                            {
                                state.AddFailure($"CHILD:{child.GetType().Name}", exChild.Message);
                            }
                            finally
                            {
                                try
                                {
                                    if (child.IsVisible)
                                    {
                                        ClickCancelOrClose(child);
                                    }

                                    if (child.IsVisible)
                                    {
                                        child.Close();
                                    }
                                }
                                catch
                                {
                                    // continue
                                }

                                WaitForUiIdle(2);
                            }
                        }
                    }

                    // Dark white surface sample on current root after action
                    if (state.Theme == AppTheme.Dark)
                    {
                        foreach (var tb in FindVisualChildren<TextBox>(root).Take(30))
                        {
                            if (IsNearWhiteBrush(tb.Background))
                            {
                                row.Detail += $" white-textbox:{tb.Name};";
                            }
                        }
                    }

                    if (row.Detail.Contains("ERROR_POPUP", StringComparison.Ordinal)
                        && !row.Detail.Contains("EXPECTED_BUSINESS_POPUP", StringComparison.Ordinal))
                    {
                        row.Status = "FAIL";
                        state.Fail++;
                        state.Failures.Add($"{buttonId} {module}/{label}: {row.Detail}");
                    }
                    else if (row.Detail.Contains("ACCESSIBILITY ISSUE", StringComparison.Ordinal)
                             && string.IsNullOrWhiteSpace(name)
                             && string.IsNullOrWhiteSpace(label))
                    {
                        row.Status = "FAIL";
                        row.Detail += " UNIDENTIFIED_BUTTON;";
                        state.Fail++;
                        state.Failures.Add($"{buttonId} {module}/{label}: {row.Detail}");
                    }
                    else
                    {
                        // outside-window / clipped / white-textbox / accessibility tip = finding, não FAIL automático
                        // se o clique foi executado sem exception.
                        row.Status = ClassifyAfterPopup(guardian, row, "");
                        if (row.Status == "FAIL")
                        {
                            state.Fail++;
                            state.Failures.Add($"{buttonId} {module}/{label}: {row.Detail}");
                        }
                        else
                        {
                            state.Pass++;
                        }
                    }

                    state.Tested++;
                    state.AddResult(row);
                }
                catch (Exception ex)
                {
                    row.Status = "FAIL";
                    row.Exception = $"{ex.GetType().Name}: {ex.Message}";
                    row.Clicked = true;
                    state.Fail++;
                    state.Tested++;
                    state.Failures.Add($"{buttonId} {module}/{label}: {row.Exception}");
                    state.AddResult(row);
                    DrainPopups(guardian, state, $"{module}/{buttonId}:fail");
                    try { CloseOwnedExcept(main); } catch { /* continue suite */ }
                    if (!IsUiAlive(root, window))
                    {
                        break;
                    }
                }
            }
        }

        private static string ClassifyAfterPopup(
            ExhaustivePopupGuardian? guardian,
            ExhaustiveButtonResult row,
            string extraDetail)
        {
            if (!string.IsNullOrEmpty(extraDetail))
            {
                row.Detail += extraDetail;
            }

            var lastText = guardian?.LastPopupText;
            if (!string.IsNullOrWhiteSpace(lastText)
                && lastText.StartsWith("EXPECTED_BUSINESS_POPUP:", StringComparison.Ordinal))
            {
                row.Detail += " EXPECTED_BUSINESS_POPUP;";
                guardian?.ClearLastErrorPopup();
                return "PASS";
            }

            var err = guardian?.ConsumeLastErrorPopup();
            if (!string.IsNullOrWhiteSpace(err))
            {
                row.Detail += $" ERROR_POPUP:{err};";
                return "FAIL";
            }

            return "PASS";
        }

        /// <summary>
        /// Espera e força o descarte de MessageBox/avisos que bloqueiam a UI thread.
        /// </summary>
        private static void DrainPopups(ExhaustivePopupGuardian? guardian, ExhaustiveRoundState state, string context)
        {
            if (guardian == null)
            {
                return;
            }

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 4000)
            {
                guardian.Pulse(context);
                if (!guardian.HasBlockingPopup())
                {
                    // ainda dá um pump curto para BeginInvoke de managed dialogs
                    WaitForUiIdle(1);
                    guardian.Pulse(context);
                    if (!guardian.HasBlockingPopup())
                    {
                        return;
                    }
                }

                Thread.Sleep(80);
                WaitForUiIdle(1);
            }

            if (guardian.HasBlockingPopup())
            {
                state.Failures.Add($"POPUP-TIMEOUT:{context}");
                App.Logger.LogWarning($"ExhaustiveUi popup ainda presente apos drain: {context}");
            }
        }

        private static bool IsUiAlive(DependencyObject root, Window? window)
        {
            try
            {
                if (window != null && (!window.IsLoaded || !window.IsVisible))
                {
                    return false;
                }

                if (root is FrameworkElement fe && !fe.IsLoaded)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsWindowClosingControl(ButtonBase button, Window? window, int depth)
        {
            var name = (button.Name ?? string.Empty).ToLowerInvariant();
            if (name is "closebutton" or "btnclose" or "btnfechar" or "cancelarbutton" or "btncancelar")
            {
                return true;
            }

            // Na MainWindow (depth 0) não tratar "Fechar"/"Cancelar" como fim da fila — pode ser ação de painel.
            if (depth == 0 && window is MainWindow)
            {
                return false;
            }

            var text = ExtractButtonBaseText(button).ToLowerInvariant().Trim();
            return text is "x" or "✕" or "×"
                || text == "fechar"
                || text == "close"
                || text == "cancelar"
                || text == "cancel"
                || text == "voltar";
        }

        /// <summary>
        /// Prioridade de execução: ações primeiro; file/print; navegação; login submit; cancel/close por último.
        /// </summary>
        private static int GetExhaustiveExecutionPriority(ButtonBase button, Window? window, int depth)
        {
            var label = ExtractButtonBaseText(button);
            var tip = button.ToolTip?.ToString() ?? string.Empty;
            var name = button.Name ?? string.Empty;

            if (IsWindowClosingControl(button, window, depth))
            {
                return 50;
            }

            if (window is LoginWindow && IsLoginSubmitControl(button, label))
            {
                return 40;
            }

            if (IsModuleNavigationControl(button, window, depth, label, tip, name))
            {
                return 30;
            }

            if (IsFilePickerOrPrintTrigger(label, tip, name))
            {
                return 10; // processar cedo → SKIPPED/NOT_TESTABLE, não ficar atrás de close
            }

            return 0;
        }

        private static bool IsModuleNavigationControl(
            ButtonBase button,
            Window? window,
            int depth,
            string label,
            string tip,
            string name)
        {
            if (depth != 0 || window is not MainWindow)
            {
                return false;
            }

            var n = name.ToLowerInvariant();
            if (n.Contains("atalho", StringComparison.Ordinal) && n.Contains("dashboard", StringComparison.Ordinal))
            {
                return true;
            }

            var text = $"{label} {tip}".ToLowerInvariant();
            return text.Contains("abrir módulo", StringComparison.Ordinal)
                   || text.Contains("abrir modulo", StringComparison.Ordinal);
        }

        private static void FlushUnreachableQueue(
            List<(ButtonBase Button, string ButtonId, string Label, string Name, string Automation, string Tip)> mapped,
            int startIndex,
            string module,
            string breadcrumb,
            ExhaustiveRoundState state,
            string status,
            string reason)
        {
            for (var i = startIndex; i < mapped.Count; i++)
            {
                var item = mapped[i];
                state.Skipped++;
                if (string.Equals(status, "BLOCKED", StringComparison.OrdinalIgnoreCase))
                {
                    state.Blocked++;
                }

                state.AddResult(new ExhaustiveButtonResult
                {
                    Status = status,
                    Module = module,
                    Window = breadcrumb,
                    ControlName = item.Name,
                    Content = item.Label,
                    AutomationName = item.Automation,
                    ToolTip = item.Tip,
                    Detail = $"{item.ButtonId} {reason}"
                });
            }
        }

        private static bool IsLoginSubmitControl(ButtonBase button, string label)
        {
            var name = (button.Name ?? string.Empty).ToLowerInvariant();
            var text = (label ?? string.Empty).ToLowerInvariant();
            return name.Contains("entrar") || name.Contains("login") || name.Contains("signin")
                || text.Contains("entrar") || text.Contains("login") || text == "ok";
        }

        private void TryFillLoginCredentials(Window login)
        {
            try
            {
                var email = _fixture?.Administrator.Email ?? "smoke-admin@primoauto.com";
                // Banco isolado do smoke: Workflow@123. Admin real (instalação) via PRIMOX_ADMIN_PASSWORD.
                var password = Environment.GetEnvironmentVariable("PRIMOX_ADMIN_PASSWORD");
                if (string.IsNullOrWhiteSpace(password))
                {
                    password = "Workflow@123";
                }

                foreach (var tb in FindVisualChildren<TextBox>(login))
                {
                    if (tb.Name?.Contains("Email", StringComparison.OrdinalIgnoreCase) == true
                        || tb.Name?.Contains("Usuario", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        tb.Text = email;
                    }
                }

                foreach (var pb in FindVisualChildren<PasswordBox>(login))
                {
                    pb.Password = password;
                }
            }
            catch
            {
                // best-effort
            }
        }

        private static List<ButtonBase> DiscoverAllButtons(DependencyObject root)
        {
            // Auditoria EXCLUSIVA de botões — sem Take()/amostragem.
            // NÃO inclui CalendarDayButton, ScrollBar RepeatButton, CheckBox, RadioButton,
            // TextBox, DataGridCell, etc.
            var list = new List<ButtonBase>();
            var seen = new HashSet<ButtonBase>();
            foreach (var b in FindVisualChildren<ButtonBase>(root))
            {
                if (!IsPrimaryActionButton(b))
                {
                    continue;
                }

                if (seen.Add(b))
                {
                    list.Add(b);
                }
            }

            return list;
        }

        /// <summary>
        /// Classifica o que entra na fila ExhaustiveButtonSimulation.
        /// </summary>
        private static bool IsPrimaryActionButton(ButtonBase b)
        {
            if (b == null || !b.IsVisible)
            {
                return false;
            }

            // Dias do calendário — NUNCA entram na fila (evita 28–31 cliques por mês).
            if (b is CalendarDayButton)
            {
                return false;
            }

            // Headers de DataGrid herdam ButtonBase — NÃO são botões de ação.
            if (b is DataGridColumnHeader || b is DataGridRowHeader)
            {
                return false;
            }

            // CheckBox / RadioButton não são botões de ação desta suíte.
            if (b is CheckBox || b is RadioButton)
            {
                return false;
            }

            // RepeatButton só conta se NÃO for seta de ScrollBar.
            if (b is RepeatButton)
            {
                return !IsInsideScrollBar(b);
            }

            // Ghost/chrome: sem texto, nome, AutomationName, ToolTip nem Command — não é ação auditável.
            var label = ExtractButtonBaseText(b);
            var automation = AutomationProperties.GetName(b) ?? string.Empty;
            var tip = b.ToolTip?.ToString() ?? string.Empty;
            var looksLikeTypeNameOnly = string.Equals(label, b.GetType().Name, StringComparison.Ordinal)
                                        || string.Equals(label, "ToggleButton", StringComparison.OrdinalIgnoreCase)
                                        || string.Equals(label, "Button", StringComparison.OrdinalIgnoreCase);
            if ((string.IsNullOrWhiteSpace(label) || looksLikeTypeNameOnly)
                && string.IsNullOrWhiteSpace(b.Name)
                && string.IsNullOrWhiteSpace(automation)
                && string.IsNullOrWhiteSpace(tip)
                && b.Command == null)
            {
                return false;
            }

            // CalendarButton (Anterior/Próximo/mês) — botão real do calendário: INCLUIR.
            return true;
        }

        private static bool IsInsideScrollBar(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is ScrollBar)
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private static string ExtractButtonBaseText(ButtonBase button)
        {
            if (button is Button btn)
            {
                return ExtractButtonText(btn);
            }

            return button.Content switch
            {
                string s => s.Trim(),
                TextBlock tb => tb.Text?.Trim() ?? button.Name ?? string.Empty,
                _ => button.Name ?? button.GetType().Name
            };
        }

        private static void InventoryFields(
            DependencyObject root,
            string module,
            string windowName,
            ExhaustiveRoundState state)
        {
            foreach (var tb in FindVisualChildren<TextBox>(root))
            {
                if (!tb.IsVisible)
                {
                    continue;
                }

                state.Fields.Add($"{module}|{windowName}|TextBox|{tb.Name}|tag={tb.Tag}");
            }

            foreach (var dp in FindVisualChildren<DatePicker>(root))
            {
                if (dp.IsVisible)
                {
                    state.Fields.Add($"{module}|{windowName}|DatePicker|{dp.Name}");
                }
            }

            foreach (var cb in FindVisualChildren<ComboBox>(root))
            {
                if (cb.IsVisible)
                {
                    state.Fields.Add($"{module}|{windowName}|ComboBox|{cb.Name}");
                }
            }
        }

        private static bool IsFilePickerOrPrintTrigger(string label, string tip, string name)
        {
            var text = $"{label} {tip} {name}".ToLowerInvariant()
                .Replace("_", " ", StringComparison.Ordinal)
                .Replace("-", " ", StringComparison.Ordinal);
            // Também colapsa CamelCase simples: SelecionarArquivo → selecionararquivo
            var compact = text.Replace(" ", "", StringComparison.Ordinal);

            string[] keys =
            {
                "procurar", "browse", "escolher arquivo", "selecionar arquivo", "abrir arquivo",
                "importar arquivo", "importar xml", "anexar", "upload", "carregar arquivo",
                "imprimir", "print", "exportar pdf", "salvar como", "openfile", "savefile",
                "selecionararquivo", "abrirarquivo", "escolherarquivo",
                "assinatura digital", "selecionar assinatura", "selecionar documento",
                "capturar assinatura", "trocar xml", "selecionarxml"
            };
            return keys.Any(k =>
                text.Contains(k, StringComparison.Ordinal)
                || compact.Contains(k.Replace(" ", "", StringComparison.Ordinal), StringComparison.Ordinal));
        }

        private static bool IsExhaustiveDestructive(string label, string tip, string name)
        {
            var text = $"{label} {tip} {name}".ToLowerInvariant();
            string[] keys =
            {
                "excluir", "delete", "apagar", "inativar", "remover", "estornar", "cancelar venda",
                "cancelar os", "reset", "formatar", "destruir", "baixar estoque",
                "sair", "logout", "log off", "encerrar sess", "trocar usu"
            };
            return keys.Any(k => text.Contains(k, StringComparison.Ordinal));
        }

        private static void ClickCancelOrClose(Window window)
        {
            var buttons = DiscoverAllButtons(window);
            foreach (var b in buttons)
            {
                var t = ExtractButtonBaseText(b).ToLowerInvariant();
                if (!b.IsEnabled || !b.IsVisible)
                {
                    continue;
                }

                if (t.Contains("cancel") || t.Contains("não") || t.Contains("nao")
                    || t.Contains("fechar") || t.Contains("close") || t.Contains("voltar")
                    || t == "não" || t == "no")
                {
                    try
                    {
                        b.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        WaitForUiIdle(2);
                        return;
                    }
                    catch
                    {
                        // try next
                    }
                }
            }
        }

        private static List<Window> SnapshotWindows()
        {
            var app = Application.Current;
            if (app == null)
            {
                return new List<Window>();
            }

            return app.Windows.OfType<Window>().Where(w => w != null && w.IsVisible).ToList();
        }

        private static List<Window> DiffWindows(List<Window> before)
        {
            var app = Application.Current;
            if (app == null)
            {
                return new List<Window>();
            }

            var beforeSet = new HashSet<Window>(before ?? new List<Window>());
            return app.Windows.OfType<Window>()
                .Where(w => w != null && w.IsVisible && !beforeSet.Contains(w))
                .ToList();
        }

        private static void ApplyWindowSize(Window window, int width, int height)
        {
            try
            {
                window.WindowState = WindowState.Normal;
                window.Width = Math.Min(width, SystemParameters.WorkArea.Width);
                window.Height = Math.Min(height, SystemParameters.WorkArea.Height);
                window.Left = Math.Max(0, (SystemParameters.WorkArea.Width - window.Width) / 2);
                window.Top = Math.Max(0, (SystemParameters.WorkArea.Height - window.Height) / 2);
            }
            catch
            {
                // ignore size failures on constrained hosts
            }
        }

        private void CloseOwnedExcept(MainWindow? main)
        {
            var app = Application.Current;
            if (app == null || app.Dispatcher?.HasShutdownStarted == true)
            {
                return;
            }

            List<Window> windows;
            try
            {
                windows = app.Windows.OfType<Window>()
                    .Where(w => w != null && w.IsVisible && !ReferenceEquals(w, main))
                    .ToList();
            }
            catch
            {
                return;
            }

            foreach (var w in windows)
            {
                try { w.Close(); } catch { /* continue */ }
            }

            WaitForUiIdle(2);
        }

        private void CloseAllOwnedExcept(MainWindow? main) => CloseOwnedExcept(main);

        private sealed class ExhaustiveRunState
        {
            public ExhaustiveRunState(string evidenceRoot, string repoEvidence)
            {
                EvidenceRoot = evidenceRoot;
                RepoEvidence = repoEvidence;
            }

            public string EvidenceRoot { get; }
            public string RepoEvidence { get; }
            public List<string> RoundsPlanned { get; } = new();
            public List<string> RoundsExecuted { get; } = new();
            public int Discovered { get; private set; }
            public int Tested { get; private set; }
            public int Pass { get; private set; }
            public int Fail { get; private set; }
            public int Skipped { get; private set; }
            public int Blocked { get; private set; }
            public List<string> Failures { get; } = new();
            public List<ExhaustiveButtonResult> Results { get; } = new();
            public HashSet<string> Pages { get; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> Windows { get; } = new(StringComparer.OrdinalIgnoreCase);
            public StringBuilder Tree { get; } = new();
            public List<string> Fields { get; } = new();
            public List<string> Popups { get; } = new();

            public double ExecutionCoveragePct =>
                Discovered == 0 ? 0 : 100.0 * Tested / Discovered;

            public void RecordRoundCrash(string roundLabel, Exception ex)
            {
                Fail++;
                Failures.Add($"ROUND:{roundLabel}: {ex.GetType().Name}: {ex.Message}");
            }

            public void Merge(ExhaustiveRoundState round)
            {
                Discovered += round.Discovered;
                Tested += round.Tested;
                Pass += round.Pass;
                Fail += round.Fail;
                Skipped += round.Skipped;
                Blocked += round.Blocked;
                Failures.AddRange(round.Failures);
                Results.AddRange(round.Results);
                foreach (var p in round.PagesVisited) Pages.Add(p);
                foreach (var w in round.WindowsVisited) Windows.Add(w);
                Tree.AppendLine($"## Round {round.Label}");
                Tree.Append(round.Tree);
                Fields.AddRange(round.Fields);
                Popups.AddRange(round.Popups);
            }

            public void WriteArtifacts()
            {
                var csv = new StringBuilder();
                csv.AppendLine("TestId,Theme,Resolution,Window,Page,ControlType,Control,Content,AutomationName,Enabled,Clicked,Result,OpenedWindow,Exception,DurationMs,Detail");
                var id = 0;
                foreach (var r in Results)
                {
                    id++;
                    csv.AppendLine(string.Join(",",
                        $"EX-{id:000000}",
                        Csv(r.Theme),
                        Csv(r.Resolution),
                        Csv(r.Window),
                        Csv(r.Module),
                        Csv(r.ControlType),
                        Csv(r.ControlName),
                        Csv(r.Content),
                        Csv(r.AutomationName),
                        r.Enabled,
                        r.Clicked,
                        Csv(r.Status),
                        Csv(r.OpenedWindow),
                        Csv(r.Exception),
                        r.DurationMs,
                        Csv(r.Detail)));
                }

                var summary = new StringBuilder();
                summary.AppendLine("# PRIMOX Exhaustive UI — Summary");
                summary.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                summary.AppendLine($"Rounds planned: {string.Join(", ", RoundsPlanned)}");
                summary.AppendLine($"Rounds executed: {string.Join(", ", RoundsExecuted)}");
                summary.AppendLine($"Pages: {Pages.Count}");
                summary.AppendLine($"Windows: {Windows.Count}");
                var executable = Tested + Blocked; // habilitados tentados + bloqueados mid-queue
                summary.AppendLine($"Buttons discovered: {Discovered}");
                summary.AppendLine($"Buttons executable (tested+blocked): {executable}");
                summary.AppendLine($"Buttons tested: {Tested}");
                summary.AppendLine($"PASS: {Pass}");
                summary.AppendLine($"FAIL: {Fail}");
                summary.AppendLine($"SKIPPED/EXPECTED_DISABLED/N/A: {Skipped}");
                summary.AppendLine($"BLOCKED: {Blocked}");
                summary.AppendLine($"Coverage tested/discovered: {ExecutionCoveragePct:F2}%");
                summary.AppendLine($"Coverage tested/executable: {(executable == 0 ? 0 : 100.0 * Tested / executable):F2}%");
                summary.AppendLine($"Functional PASS rate (PASS/tested): {(Tested == 0 ? 0 : 100.0 * Pass / Tested):F2}%");
                summary.AppendLine($"Popups dismissed: {Popups.Count}");
                summary.AppendLine("Note: CalendarDayButton days excluded by audit rule; native file/print = SKIPPED/NOT_TESTABLE.");
                summary.AppendLine();
                summary.AppendLine("## Failures");
                foreach (var f in Failures.Take(100))
                {
                    summary.AppendLine($"- {f}");
                }

                summary.AppendLine();
                summary.AppendLine("## Popups (text captured)");
                foreach (var p in Popups.Take(200))
                {
                    summary.AppendLine($"- {p}");
                }

                File.WriteAllText(Path.Combine(EvidenceRoot, "exhaustive-summary.md"), summary.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(EvidenceRoot, "exhaustive-buttons.csv"), csv.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(EvidenceRoot, "exhaustive-tree.md"), Tree.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(EvidenceRoot, "exhaustive-fields.txt"), string.Join(Environment.NewLine, Fields.Distinct()), Encoding.UTF8);
                File.WriteAllText(Path.Combine(EvidenceRoot, "exhaustive-popups.txt"), string.Join(Environment.NewLine, Popups), Encoding.UTF8);

                try
                {
                    File.Copy(Path.Combine(EvidenceRoot, "exhaustive-summary.md"), Path.Combine(RepoEvidence, "exhaustive-summary-latest.md"), true);
                    File.Copy(Path.Combine(EvidenceRoot, "exhaustive-buttons.csv"), Path.Combine(RepoEvidence, "exhaustive-buttons-latest.csv"), true);
                    File.Copy(Path.Combine(EvidenceRoot, "exhaustive-tree.md"), Path.Combine(RepoEvidence, "exhaustive-tree-latest.md"), true);
                    File.Copy(Path.Combine(EvidenceRoot, "exhaustive-popups.txt"), Path.Combine(RepoEvidence, "exhaustive-popups-latest.txt"), true);
                }
                catch
                {
                    // repo copy best-effort
                }
            }

            private static string Csv(string? value)
            {
                var v = (value ?? string.Empty).Replace("\"", "\"\"");
                return $"\"{v}\"";
            }
        }

        private sealed class ExhaustiveRoundState
        {
            public ExhaustiveRoundState(string label, AppTheme theme, int width, int height)
            {
                Label = label;
                Theme = theme;
                Width = width;
                Height = height;
            }

            public string Label { get; }
            public AppTheme Theme { get; }
            public int Width { get; }
            public int Height { get; }
            public int Discovered { get; set; }
            public int Tested { get; set; }
            public int Pass { get; set; }
            public int Fail { get; set; }
            public int Skipped { get; set; }
            public int Blocked { get; set; }
            public List<string> Failures { get; } = new();
            public List<ExhaustiveButtonResult> Results { get; } = new();
            public HashSet<string> PagesVisited { get; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> WindowsVisited { get; } = new(StringComparer.OrdinalIgnoreCase);
            public StringBuilder Tree { get; } = new();
            public List<string> Fields { get; } = new();
            public List<string> Popups { get; } = new();
            private int _buttonSeq;
            public string CurrentModule { get; set; } = "";
            public Dictionary<string, int> SignatureCounts { get; } = new(StringComparer.Ordinal);

            public int NextButtonId() => Interlocked.Increment(ref _buttonSeq);

            public void AddResult(ExhaustiveButtonResult row) => Results.Add(row);

            public void AddFailure(string id, string detail)
            {
                Fail++;
                Failures.Add($"{id}: {detail}");
            }

            public void RecordPopup(string context, string text, string action)
            {
                var line = $"{DateTime.Now:HH:mm:ss.fff}|{Label}|{context}|action={action}|{text}";
                Popups.Add(line);
                App.Logger.LogInfo($"ExhaustiveUi POPUP: {line}", "Smoke");
            }
        }

        /// <summary>
        /// Guardião de pop-ups: lê texto de MessageBox/avisos e fecha com segurança
        /// para a suíte ExhaustiveUi não travar na UI thread.
        /// </summary>
        private sealed class ExhaustivePopupGuardian : IDisposable
        {
            private readonly ExhaustiveRoundState _state;
            private readonly Func<Window?> _ownerResolver;
            private readonly CancellationTokenSource _cts = new();
            private readonly HashSet<Window> _protected = new();
            private readonly object _gate = new();
            private Thread? _worker;
            private volatile bool _nativePopupPresent;
            private string? _lastErrorPopup;
            private string? _lastPopupText;
            private Action<Window>? _modalExplorer;
            private readonly HashSet<Window> _modalExplored = new();

            public ExhaustivePopupGuardian(ExhaustiveRoundState state, Func<Window?> ownerResolver)
            {
                _state = state;
                _ownerResolver = ownerResolver;
            }

            /// <summary>
            /// Explora janelas abertas via ShowDialog (senão RaiseEvent fica bloqueado e a fila trava).
            /// </summary>
            public void SetModalExplorer(Action<Window> explorer) => _modalExplorer = explorer;

            public void ClearLastErrorPopup()
            {
                _lastErrorPopup = null;
                _lastPopupText = null;
            }

            public string? ConsumeLastErrorPopup()
            {
                var err = _lastErrorPopup;
                _lastErrorPopup = null;
                return err;
            }

            public string? LastPopupText => _lastPopupText;

            public void Start()
            {
                if (_worker != null)
                {
                    return;
                }

                _worker = new Thread(Run)
                {
                    IsBackground = true,
                    Name = "ExhaustiveUiPopupGuardian"
                };
                _worker.Start();
            }

            public void ProtectWindow(Window? window)
            {
                if (window == null)
                {
                    return;
                }

                lock (_gate)
                {
                    _protected.Add(window);
                }
            }

            public bool HasBlockingPopup() => _nativePopupPresent || HasManagedAlert();

            public void Pulse(string context)
            {
                try
                {
                    DismissNative(context);
                    var dispatcher = Application.Current?.Dispatcher;
                    if (dispatcher == null || dispatcher.HasShutdownStarted)
                    {
                        return;
                    }

                    // Evita deadlock: se já estamos na UI thread, chamar direto.
                    if (dispatcher.CheckAccess())
                    {
                        DismissManagedAlerts(context);
                    }
                    else
                    {
                        dispatcher.Invoke(() => DismissManagedAlerts(context), DispatcherPriority.Send);
                    }
                }
                catch
                {
                    // continue
                }
            }

            public void Dispose()
            {
                _cts.Cancel();
                if (_worker != null && _worker.IsAlive)
                {
                    _worker.Join(TimeSpan.FromSeconds(2));
                }
            }

            private void Run()
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        DismissNative("bg");
                        Application.Current?.Dispatcher.BeginInvoke(
                            new Action(() => DismissManagedAlerts("bg")),
                            DispatcherPriority.Background);
                    }
                    catch
                    {
                        // ignore
                    }

                    Thread.Sleep(100);
                }
            }

            private static string ClassifyPopupText(string text)
            {
                var t = text.ToLowerInvariant();

                // Regra de negócio esperada — não é crash do produto.
                if (t.Contains("exclusao bloqueada")
                    || t.Contains("exclusão bloqueada")
                    || t.Contains("possui ordens de servico")
                    || t.Contains("possui ordens de serviço")
                    || t.Contains("informe o arquivo")
                    || t.Contains("informe o nome")
                    || t.Contains("cadastro incompleto")
                    || t.Contains("nao ha itens")
                    || t.Contains("não há itens")
                    || t.Contains("nenhuma importacao")
                    || t.Contains("nenhuma importação")
                    || t.Contains("nao possui os vinculada")
                    || t.Contains("não possui os vinculada")
                    || t.Contains("veiculoid valido")
                    || t.Contains("clienteid valido"))
                {
                    return "expected-business";
                }

                if (t.Contains("exception")
                    || t.Contains("stack")
                    || t.Contains("sqlite")
                    || t.Contains("unhandled")
                    || t.Contains("invalidoperation")
                    || t.Contains("nullreference")
                    || t.Contains("erro inesperado"))
                {
                    return "error";
                }

                if (t.Contains("deseja")
                    || t.Contains("confirma")
                    || t.Contains("excluir")
                    || t.Contains("apagar")
                    || t.Contains("cancelar"))
                {
                    return "confirm";
                }

                return "info";
            }

            private void DismissNative(string context)
            {
                var found = false;
                NativeMethods.EnumerateWindows(handle =>
                {
                    if (!NativeMethods.IsWindowVisible(handle))
                    {
                        return true;
                    }

                    NativeMethods.GetWindowThreadProcessId(handle, out var processId);
                    if (processId != Environment.ProcessId)
                    {
                        return true;
                    }

                    var className = NativeMethods.GetClassName(handle);
                    var title = NativeMethods.GetWindowText(handle);

                    // MessageBox clássico
                    if (string.Equals(className, "#32770", StringComparison.Ordinal))
                    {
                        found = true;
                        var text = NativeMethods.ReadDialogText(handle);
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            text = string.IsNullOrWhiteSpace(title) ? "(empty MessageBox)" : $"title={title}";
                        }

                        _lastPopupText = text;
                        var kind = ClassifyPopupText(text);
                        if (kind == "error")
                        {
                            _lastErrorPopup = text;
                        }
                        else if (kind == "expected-business")
                        {
                            _lastPopupText = "EXPECTED_BUSINESS_POPUP:" + text;
                        }

                        _state.RecordPopup(context, text, $"native-scan+dismiss kind={kind}");
                        NativeMethods.TryDismissDialogSafe(handle, allowYes: false);
                        return true;
                    }

                    // OpenFileDialog / SaveFileDialog / impressão — travam RaiseEvent se não fechados.
                    if (IsNativeFileOrSystemDialog(className, title))
                    {
                        found = true;
                        _lastPopupText = $"title={title}; class={className}";
                        _state.RecordPopup(context, _lastPopupText, "native-file-dialog-close");
                        NativeMethods.SendClose(handle);
                        return true;
                    }

                    return true;
                });

                _nativePopupPresent = found;
            }

            private static bool IsNativeFileOrSystemDialog(string className, string title)
            {
                var t = (title ?? string.Empty).Trim();
                var c = className ?? string.Empty;

                // Janelas WPF (HwndWrapper) NÃO são file/print dialogs nativos.
                // Ex.: "Abrir caixa" (OperacaoCaixaWindow) começava com "Abrir " e recebia SendClose → crash.
                if (c.StartsWith("HwndWrapper", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (t.Length == 0 && !c.Contains("Dialog", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Diálogos comuns do Win32 / Common Item Dialog
                if (string.Equals(t, "Abrir", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, "Open", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, "Salvar", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, "Salvar como", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, "Save As", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t, "Save", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("Imprimir", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("Print", StringComparison.OrdinalIgnoreCase)
                    || t.StartsWith("Abrir ", StringComparison.OrdinalIgnoreCase)
                    || t.StartsWith("Open ", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return c.Equals("CabinetWClass", StringComparison.OrdinalIgnoreCase)
                       || c.Equals("#32770", StringComparison.OrdinalIgnoreCase);
            }

            private bool HasManagedAlert()
            {
                try
                {
                    var owner = _ownerResolver();
                    return Application.Current?.Windows.OfType<Window>()
                        .Any(w => w.IsVisible
                                  && !ReferenceEquals(w, owner)
                                  && w is not MainWindow
                                  && IsLikelyAlert(w)
                                  && !IsProtected(w)) == true;
                }
                catch
                {
                    return false;
                }
            }

            private bool IsProtected(Window window)
            {
                lock (_gate)
                {
                    return _protected.Contains(window);
                }
            }

            private void DismissManagedAlerts(string context)
            {
                var owner = _ownerResolver();
                var windows = Application.Current?.Windows.OfType<Window>()
                    .Where(w => w.IsVisible && !ReferenceEquals(w, owner) && w is not MainWindow)
                    .ToList() ?? new List<Window>();

                foreach (var window in windows)
                {
                    // Diálogos conhecidos (filial, confirmação, etc.) — tratamento específico primeiro.
                    if (TryHandleKnownBlockingDialog(window, context))
                    {
                        continue;
                    }

                    // ShowDialog modal genérico: explorar botões no pump aninhado e fechar.
                    if (TryExploreAndCloseModal(window, context))
                    {
                        continue;
                    }

                    if (IsProtected(window) && !IsLikelyAlert(window))
                    {
                        continue;
                    }

                    // Janelas de edição grandes sob exploração: não fechar automaticamente.
                    if (IsProtected(window) && window.ActualWidth > 480 && window.ActualHeight > 320)
                    {
                        continue;
                    }

                    if (!IsLikelyAlert(window) && context == "bg")
                    {
                        continue;
                    }

                    var body = ReadManagedWindowText(window);
                    if (window is ConfirmacaoCriticaWindow)
                    {
                        _state.RecordPopup(context, body, "critica-cancel");
                        ClickCancelOrClose(window);
                        continue;
                    }

                    if (IsLikelyAlert(window) || context != "bg")
                    {
                        _state.RecordPopup(context, body, "managed-dismiss");
                        if (!TryClickDismissButton(window))
                        {
                            try { window.Close(); } catch { /* ignore */ }
                        }
                    }
                }
            }

            private bool TryExploreAndCloseModal(Window window, string context)
            {
                if (_modalExplorer == null || window is MainWindow || window is LoginWindow)
                {
                    return false;
                }

                if (IsProtected(window))
                {
                    return false;
                }

                // Já explorada nesta cadeia — só garantir fechamento.
                lock (_gate)
                {
                    if (_modalExplored.Contains(window))
                    {
                        try { if (window.IsVisible) window.Close(); } catch { /* ignore */ }
                        return true;
                    }

                    _modalExplored.Add(window);
                }

                try
                {
                    _state.RecordPopup(context, ReadManagedWindowText(window), "modal-explore-then-close");
                    ProtectWindow(window);
                    _modalExplorer(window);
                }
                catch (Exception ex)
                {
                    _state.RecordPopup(context, $"{window.GetType().Name}: {ex.Message}", "modal-explore-error");
                }
                finally
                {
                    try
                    {
                        if (window.IsVisible)
                        {
                            if (!TryClickLabeled(window, "Cancelar", "Fechar", "Close", "Voltar"))
                            {
                                window.Close();
                            }
                        }
                    }
                    catch
                    {
                        try { window.Close(); } catch { /* ignore */ }
                    }

                    WaitForUiIdle(1);
                }

                return true;
            }

            private bool TryHandleKnownBlockingDialog(Window window, string context)
            {
                try
                {
                    if (window is SelecaoFilialWindow)
                    {
                        _state.RecordPopup(context, ReadManagedWindowText(window), "selecao-filial-confirm");
                        SelectFirstComboOrList(window);
                        if (!TryClickLabeled(window, "Confirmar", "OK", "Ok", "Selecionar", "Continuar"))
                        {
                            try { window.DialogResult = true; } catch { /* ignore */ }
                            try { window.Close(); } catch { /* ignore */ }
                        }

                        return true;
                    }

                    if (window is OperacaoCaixaWindow)
                    {
                        _state.RecordPopup(context, ReadManagedWindowText(window), "operacao-caixa-cancel");
                        ClickCancelOrClose(window);
                        return true;
                    }

                    if (window is SelecionarClientePDVWindow
                        || window is SelecionarVendaWindow
                        || window is SelecionarOrcamentoWindow)
                    {
                        _state.RecordPopup(context, ReadManagedWindowText(window), "selecao-cancel");
                        if (!TryClickLabeled(window, "Cancelar", "Fechar", "Voltar"))
                        {
                            try { window.Close(); } catch { /* ignore */ }
                        }

                        return true;
                    }

                    if (window is ConfirmacaoCriticaWindow)
                    {
                        _state.RecordPopup(context, ReadManagedWindowText(window), "critica-cancel");
                        ClickCancelOrClose(window);
                        return true;
                    }

                    // Janelas de importação/arquivo: mapear botões via ExploreSurface; se o bg
                    // encontrar OpenFileDialog filho, o dismiss nativo já fecha. Aqui garante
                    // Cancelar/Fechar se a janela ficar órfã sem exploração ativa.
                    if (window is ImportarCatalogoPecasWindow && context.Contains("fail", StringComparison.OrdinalIgnoreCase))
                    {
                        _state.RecordPopup(context, ReadManagedWindowText(window), "import-catalogo-cancel");
                        if (!TryClickLabeled(window, "Cancelar", "Fechar", "Voltar"))
                        {
                            try { window.Close(); } catch { /* ignore */ }
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _state.RecordPopup(context, $"{window.GetType().Name}: {ex.Message}", "known-dialog-error");
                }

                return false;
            }

            private static void SelectFirstComboOrList(DependencyObject root)
            {
                foreach (var combo in FindVisualChildren<ComboBox>(root))
                {
                    if (combo.Items.Count > 0 && combo.SelectedIndex < 0)
                    {
                        combo.SelectedIndex = 0;
                    }
                }

                foreach (var list in FindVisualChildren<ListBox>(root))
                {
                    if (list.Items.Count > 0 && list.SelectedIndex < 0)
                    {
                        list.SelectedIndex = 0;
                    }
                }

                foreach (var grid in FindVisualChildren<DataGrid>(root))
                {
                    if (grid.Items.Count > 0 && grid.SelectedIndex < 0)
                    {
                        grid.SelectedIndex = 0;
                    }
                }
            }

            private static bool TryClickLabeled(Window window, params string[] labels)
            {
                var buttons = DiscoverAllButtons(window);
                foreach (var want in labels)
                {
                    var match = buttons.FirstOrDefault(b =>
                        b.IsEnabled && b.IsVisible
                        && string.Equals(ExtractButtonBaseText(b), want, StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                    {
                        continue;
                    }

                    try
                    {
                        match.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        return true;
                    }
                    catch
                    {
                        // next
                    }
                }

                return false;
            }

            private static bool IsLikelyAlert(Window window)
            {
                var typeName = window.GetType().Name;
                if (typeName.Contains("Confirm", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Aviso", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Alert", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Message", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Dialog", StringComparison.OrdinalIgnoreCase)
                    || window is ConfirmacaoCriticaWindow)
                {
                    return true;
                }

                // MessageBox-like: janela pequena com OK/Fechar
                if (window.ActualWidth > 0 && window.ActualWidth < 560 && window.ActualHeight > 0 && window.ActualHeight < 320)
                {
                    var texts = DiscoverAllButtons(window).Select(ExtractButtonBaseText).ToList();
                    return texts.Any(t =>
                        t.Equals("OK", StringComparison.OrdinalIgnoreCase)
                        || t.Equals("Ok", StringComparison.OrdinalIgnoreCase)
                        || t.Equals("Fechar", StringComparison.OrdinalIgnoreCase)
                        || t.Equals("Cancelar", StringComparison.OrdinalIgnoreCase)
                        || t.Equals("Não", StringComparison.OrdinalIgnoreCase)
                        || t.Equals("Nao", StringComparison.OrdinalIgnoreCase));
                }

                return false;
            }

            private static string ReadManagedWindowText(Window window)
            {
                try
                {
                    var parts = new List<string> { $"win={window.GetType().Name}", $"title={window.Title}" };
                    foreach (var tb in FindVisualChildren<TextBlock>(window).Take(12))
                    {
                        var t = tb.Text?.Trim();
                        if (!string.IsNullOrWhiteSpace(t) && t.Length > 1 && t.Length < 400)
                        {
                            parts.Add(t);
                        }
                    }

                    return string.Join(" | ", parts.Distinct());
                }
                catch (Exception ex)
                {
                    return $"win={window.GetType().Name}; read-error={ex.Message}";
                }
            }

            private static bool TryClickDismissButton(Window window)
            {
                string[] prefer =
                {
                    "Cancelar", "Não", "Nao", "No", "Fechar", "Close", "OK", "Ok", "Voltar"
                };

                var buttons = DiscoverAllButtons(window);
                foreach (var want in prefer)
                {
                    var match = buttons.FirstOrDefault(b =>
                        b.IsEnabled && b.IsVisible
                        && string.Equals(ExtractButtonBaseText(b), want, StringComparison.OrdinalIgnoreCase));
                    if (match == null)
                    {
                        continue;
                    }

                    try
                    {
                        match.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        return true;
                    }
                    catch
                    {
                        // try next
                    }
                }

                return false;
            }
        }

        private sealed class ExhaustiveButtonResult
        {
            public string Theme { get; set; } = "";
            public string Resolution { get; set; } = "";
            public string Module { get; set; } = "";
            public string Window { get; set; } = "";
            public string ControlType { get; set; } = "";
            public string ControlName { get; set; } = "";
            public string Content { get; set; } = "";
            public string AutomationName { get; set; } = "";
            public string ToolTip { get; set; } = "";
            public bool Enabled { get; set; }
            public bool Visible { get; set; }
            public bool Clicked { get; set; }
            public string Status { get; set; } = "";
            public string OpenedWindow { get; set; } = "";
            public string Exception { get; set; } = "";
            public long DurationMs { get; set; }
            public string Detail { get; set; } = "";
        }
    }
}
