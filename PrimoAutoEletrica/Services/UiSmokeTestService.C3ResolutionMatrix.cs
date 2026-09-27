using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// C3 Final Closure — explicit 8-row resolution × Light/Dark matrix.
    /// Filter: Resolucao | C3ResolutionMatrix | ResolutionMatrix
    /// Surfaces: Dashboard, Search (BuscarPrimox), Assist/Evidence (Assist tab), ExternalAiSettingsWindow.
    /// </summary>
    public sealed partial class UiSmokeTestService
    {
        private void RunC3ResolutionMatrixChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            var rounds = new (AppTheme Theme, int Width, int Height, string Label)[]
            {
                (AppTheme.Light, 1280, 720, "Light-1280x720"),
                (AppTheme.Dark, 1280, 720, "Dark-1280x720"),
                (AppTheme.Light, 1366, 768, "Light-1366x768"),
                (AppTheme.Dark, 1366, 768, "Dark-1366x768"),
                (AppTheme.Light, 1600, 900, "Light-1600x900"),
                (AppTheme.Dark, 1600, 900, "Dark-1600x900"),
                (AppTheme.Light, 1920, 1080, "Light-1920x1080"),
                (AppTheme.Dark, 1920, 1080, "Dark-1920x1080"),
            };

            var evidenceDir = Path.Combine(
                App.RuntimeAppDataPath,
                "Logs",
                "c3-resolution-matrix",
                DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(evidenceDir);
            var summary = new StringBuilder();
            summary.AppendLine("# C3 Resolution Matrix (explicit 8)");
            summary.AppendLine($"GeneratedAt={DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
            summary.AppendLine($"EvidenceDir={evidenceDir}");
            summary.AppendLine();

            foreach (var round in rounds)
            {
                var checkName = $"Resolucao:{round.Label}";
                RunCheck(result, checkName, () =>
                {
                    var themeService = new ThemeService();
                    var originalTheme = themeService.GetCurrentTheme();
                    MainWindow? window = null;
                    ExternalAiSettingsWindow? aiWindow = null;
                    var findings = new List<string>();

                    try
                    {
                        themeService.ApplyTheme(round.Theme);
                        WaitForUiIdle();

                        window = new MainWindow(syntheticUser);
                        ShowWindowForInteraction(window);
                        ApplyWindowSize(window, round.Width, round.Height);
                        WaitForUiIdle();

                        if (window.ActualWidth < Math.Min(round.Width, SystemParameters.WorkArea.Width) - 80 ||
                            window.ActualHeight < Math.Min(round.Height, SystemParameters.WorkArea.Height) - 80)
                        {
                            findings.Add($"layout-size-mismatch actual={window.ActualWidth:F0}x{window.ActualHeight:F0} requested={round.Width}x{round.Height}");
                        }

                        // Dashboard
                        if (!window.NavigateToModuleForAutomation("Dashboard", forceReload: true) ||
                            window.CurrentContentElement is not DashboardControl)
                        {
                            throw new InvalidOperationException($"{round.Label}: Dashboard failed to load.");
                        }
                        WaitForUiIdle();
                        findings.AddRange(CollectLayoutFindings(window, "Dashboard"));

                        // BaseConhecimento — Search + Assist/Evidence
                        if (!window.NavigateToModuleForAutomation("BaseConhecimento", forceReload: true) ||
                            window.CurrentContentElement is not BaseConhecimentoControl baseConhecimento)
                        {
                            throw new InvalidOperationException($"{round.Label}: BaseConhecimento failed to load.");
                        }
                        WaitForUiIdle();
                        findings.AddRange(CollectLayoutFindings(window, "BaseConhecimento"));

                        var tabs = FindElementByName<TabControl>(baseConhecimento, "MainKnowledgeTabControl")
                            ?? throw new InvalidOperationException($"{round.Label}: MainKnowledgeTabControl missing.");

                        // Search tab (index 0 = BuscarPrimox)
                        tabs.SelectedIndex = 0;
                        WaitForUiIdle();
                        var search = FindElementByName<PrimoxKnowledgeSearchControl>(baseConhecimento, "PrimoxSearchControl")
                            ?? throw new InvalidOperationException($"{round.Label}: PrimoxSearchControl missing after selecting BuscarPrimox tab.");

                        // Assist / Evidence tab (last tab = PRIMOX Assist Copilot)
                        tabs.SelectedIndex = tabs.Items.Count - 1;
                        WaitForUiIdle();
                        var assistQuery = FindElementByName<TextBox>(baseConhecimento, "AssistQueryTextBox")
                            ?? throw new InvalidOperationException($"{round.Label}: AssistQueryTextBox missing.");
                        var assistEvidence = FindElementByName<TextBlock>(baseConhecimento, "AssistEvidenceListTextBlock")
                            ?? throw new InvalidOperationException($"{round.Label}: AssistEvidenceListTextBlock missing.");
                        var assistSettingsBtn = FindElementByName<Button>(baseConhecimento, "AssistExternalSettingsButton");
                        if (assistSettingsBtn == null)
                        {
                            findings.Add("INFO:AssistExternalSettingsButton missing");
                        }

                        // Evidence / Provider settings UI (local, no live HTTP)
                        aiWindow = new ExternalAiSettingsWindow();
                        ShowWindowForInteraction(aiWindow);
                        ApplyWindowSize(aiWindow, Math.Min(round.Width, 900), Math.Min(round.Height, 700));
                        WaitForUiIdle();
                        if (!aiWindow.IsVisible)
                        {
                            throw new InvalidOperationException($"{round.Label}: ExternalAiSettingsWindow not visible.");
                        }
                        findings.AddRange(CollectLayoutFindings(aiWindow, "ExternalAiSettings"));

                        // Touch searched surfaces so unused locals are intentional evidence
                        _ = search.IsVisible;
                        _ = assistQuery.IsVisible;
                        _ = assistEvidence.IsVisible;

                        var hardFails = findings.Where(f => f.Contains("HARD:", StringComparison.Ordinal)).ToList();
                        var line = $"{round.Label} theme={round.Theme} size={round.Width}x{round.Height} findings={findings.Count} hard={hardFails.Count}";
                        summary.AppendLine(line);
                        if (findings.Count > 0)
                        {
                            summary.AppendLine("  " + string.Join(" | ", findings.Take(12)));
                        }

                        File.WriteAllText(
                            Path.Combine(evidenceDir, $"{round.Label}.txt"),
                            string.Join(Environment.NewLine, findings.DefaultIfEmpty("none")),
                            Encoding.UTF8);

                        if (hardFails.Count > 0)
                        {
                            throw new InvalidOperationException(
                                $"{round.Label}: hard layout failures: " + string.Join(" | ", hardFails.Take(8)));
                        }
                    }
                    finally
                    {
                        try { aiWindow?.Close(); } catch { /* ignore */ }
                        try
                        {
                            if (window?.IsVisible == true)
                            {
                                window.Close();
                            }
                        }
                        catch { /* ignore */ }
                        try { themeService.ApplyTheme(originalTheme); } catch { /* ignore */ }
                        WaitForUiIdle();
                    }
                });
            }

            File.WriteAllText(Path.Combine(evidenceDir, "matrix-summary.md"), summary.ToString(), Encoding.UTF8);

            try
            {
                var repo = Path.GetFullPath(Path.Combine(
                    Path.GetDirectoryName(typeof(App).Assembly.Location) ?? ".",
                    "..", "..", "..", "..", "TestResults", "UiSmoke", "C3ResolutionMatrix"));
                Directory.CreateDirectory(repo);
                File.Copy(
                    Path.Combine(evidenceDir, "matrix-summary.md"),
                    Path.Combine(repo, "matrix-summary-latest.md"),
                    true);
            }
            catch
            {
                // non-fatal
            }
        }

        private static IEnumerable<string> CollectLayoutFindings(DependencyObject root, string surface)
        {
            var findings = new List<string>();
            var stack = new Stack<DependencyObject>();
            stack.Push(root);
            var inspected = 0;

            while (stack.Count > 0 && inspected < 400)
            {
                var current = stack.Pop();
                inspected++;

                if (current is FrameworkElement fe)
                {
                    if (fe.IsVisible && fe.ActualWidth > 0 && fe.ActualWidth < 8 && fe is ButtonBase)
                    {
                        findings.Add($"HARD:{surface}:clipped-width name={fe.Name} w={fe.ActualWidth:F1}");
                    }

                    // Overflow vs host bounds = INFO (scrollable shells / sidebar menus), matching ExhaustiveUi:
                    // outside-window findings are recorded, not automatic FAIL.
                    if (fe.IsVisible && root is FrameworkElement host && (fe is Button or TextBox or ComboBox))
                    {
                        try
                        {
                            var topLeft = fe.TranslatePoint(new Point(0, 0), host);
                            if (topLeft.X < -2 || topLeft.Y < -2 ||
                                topLeft.X > host.ActualWidth + 2 ||
                                topLeft.Y > host.ActualHeight + 2)
                            {
                                var tag = string.IsNullOrWhiteSpace(fe.Name) ? fe.GetType().Name : fe.Name;
                                findings.Add($"INFO:{surface}:overflow {tag} at {topLeft.X:F0},{topLeft.Y:F0}");
                            }
                        }
                        catch
                        {
                            // transform may fail for disconnected visuals
                        }
                    }
                }

                var count = VisualTreeHelper.GetChildrenCount(current);
                for (var i = 0; i < count; i++)
                {
                    stack.Push(VisualTreeHelper.GetChild(current, i));
                }
            }

            if (root is FrameworkElement rootFe && rootFe.ActualWidth < 100)
            {
                findings.Add($"HARD:{surface}:root-too-narrow w={rootFe.ActualWidth:F0}");
            }

            return findings;
        }
    }
}
