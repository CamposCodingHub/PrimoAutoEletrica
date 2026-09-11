using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// COMMERCIAL-09.5 Overnight Full Product QA — stress harness (no product features).
    /// Filters: OvernightQa | Commercial095 | OvernightFullQa
    /// </summary>
    public sealed partial class UiSmokeTestService
    {
        private void RunOvernightQaChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            GarantirBancoIsoladoDoSmoke("PRIMOX Overnight QA");
            _fixture ??= EnsureSmokeFixture(syntheticUser);

            var navCycles = ReadOvernightInt("PRIMOX_OVERNIGHT_NAV_CYCLES", 20, min: 5, max: 100);
            var langCycles = ReadOvernightInt("PRIMOX_OVERNIGHT_LANG_CYCLES", 20, min: 6, max: 100);

            RunCheck(result, "OvernightQa:NavigationStress", () =>
            {
                var modules = ObterDeepQaModules();
                var themeService = new ThemeService();
                var originalTheme = themeService.GetCurrentTheme();
                MainWindow? window = null;
                var navigations = 0;
                var sw = Stopwatch.StartNew();
                long peakWs = 0;

                try
                {
                    themeService.ApplyTheme(AppTheme.Light);
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    for (var ciclo = 1; ciclo <= navCycles; ciclo++)
                    {
                        var tema = ciclo % 2 == 1 ? AppTheme.Light : AppTheme.Dark;
                        themeService.ApplyTheme(tema);
                        WaitForUiIdle();

                        foreach (var modulo in modules)
                        {
                            if (!window.NavigateToModuleForAutomation(modulo, forceReload: true))
                            {
                                throw new InvalidOperationException(
                                    $"OvernightQa nav ciclo {ciclo}: falha ao abrir {modulo} ({tema}).");
                            }

                            navigations++;
                            WaitForUiIdle();
                            if (window.CurrentContentElement is not FrameworkElement)
                            {
                                throw new InvalidOperationException(
                                    $"OvernightQa: conteudo nulo em {modulo} ciclo {ciclo}.");
                            }
                        }

                        if (!window.NavigateToModuleForAutomation("Dashboard", forceReload: true))
                        {
                            throw new InvalidOperationException(
                                $"OvernightQa ciclo {ciclo}: retorno Dashboard falhou.");
                        }

                        using var proc = Process.GetCurrentProcess();
                        proc.Refresh();
                        peakWs = Math.Max(peakWs, proc.WorkingSet64);
                    }
                }
                finally
                {
                    try { themeService.ApplyTheme(originalTheme); } catch { /* ignore */ }
                    try { window?.Close(); } catch { /* ignore */ }
                    CloseAllOwnedExcept(null);
                }

                sw.Stop();
                if (navigations < navCycles * modules.Count)
                {
                    throw new InvalidOperationException(
                        $"OvernightQa: navegacoes insuficientes ({navigations}).");
                }

                App.Logger.LogInfo(
                    $"OvernightQa nav: cycles={navCycles} navigations={navigations} " +
                    $"elapsed={sw.Elapsed.TotalSeconds:F1}s peakWS={peakWs / (1024 * 1024)}MB",
                    "Smoke");
            });

            RunCheck(result, "OvernightQa:LanguageStress", () =>
            {
                var helper = LocalizationHelper.Instance;
                var loc = LocalizationService.Instance;
                var original = loc.CurrentLanguageCode;
                var sequence = new[]
                {
                    "pt-BR", "en-US",
                    "en-US", "es-ES",
                    "es-ES", "pt-BR",
                    "pt-BR", "es-ES",
                    "es-ES", "en-US",
                    "en-US", "pt-BR"
                };

                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);

                    for (var i = 0; i < langCycles; i++)
                    {
                        var lang = sequence[i % sequence.Length];
                        helper.SetLanguage(lang);
                        WaitForUiIdle();

                        if (lang is "pt-BR" or "en-US" or "es-ES"
                            && !loc.CurrentLanguageCode.StartsWith(lang.Substring(0, 2), StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException(
                                $"OvernightQa lang: esperado {lang}, atual {loc.CurrentLanguageCode}");
                        }

                        if (!window.NavigateToModuleForAutomation("Dashboard", forceReload: true)
                            || !window.NavigateToModuleForAutomation("Clientes", forceReload: true)
                            || !window.NavigateToModuleForAutomation("PDV", forceReload: true))
                        {
                            throw new InvalidOperationException(
                                $"OvernightQa lang ciclo {i}: navegacao falhou em {lang}.");
                        }
                    }

                    helper.SetLanguage("xx-INVALID");
                    WaitForUiIdle();
                    if (!loc.CurrentLanguageCode.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"OvernightQa fallback: apos idioma invalido esperado pt*, atual {loc.CurrentLanguageCode}");
                    }

                    helper.SetLanguage("pt-BR");
                    WaitForUiIdle();
                }
                finally
                {
                    try { helper.SetLanguage(original); } catch { /* ignore */ }
                    try { window?.Close(); } catch { /* ignore */ }
                    CloseAllOwnedExcept(null);
                }

                App.Logger.LogInfo($"OvernightQa language stress: cycles={langCycles}", "Smoke");
            });

            RunCheck(result, "OvernightQa:DialogOpenCloseStress", () =>
            {
                MainWindow? window = null;
                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    if (!window.NavigateToModuleForAutomation("Clientes", forceReload: true))
                    {
                        throw new InvalidOperationException("OvernightQa dialog: Clientes nao abriu.");
                    }

                    WaitForUiIdle();
                    for (var i = 0; i < 15; i++)
                    {
                        CloseAllOwnedExcept(window);
                        WaitForUiIdle();
                        var stray = Application.Current.Windows
                            .OfType<Window>()
                            .Count(w => w.IsVisible && !ReferenceEquals(w, window));
                        if (stray > 2)
                        {
                            throw new InvalidOperationException(
                                $"OvernightQa dialog: overlays residuais ({stray}) apos ciclo {i}.");
                        }
                    }
                }
                finally
                {
                    try { window?.Close(); } catch { /* ignore */ }
                    CloseAllOwnedExcept(null);
                }
            });
        }

        private static int ReadOvernightInt(string envName, int defaultValue, int min, int max)
        {
            var raw = Environment.GetEnvironmentVariable(envName);
            if (string.IsNullOrWhiteSpace(raw)
                || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                return defaultValue;
            }

            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
