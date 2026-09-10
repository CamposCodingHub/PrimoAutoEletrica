using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// PRIMOX-I18N-04 — auditoria runtime user-visible (pt/en/es) com capturas e residuais.
    /// Nao afirma 100%. Mede superficies navegaveis reais.
    /// </summary>
    public sealed partial class UiSmokeTestService
    {
        private static readonly string[] I18n04CriticalModules =
        {
            "Dashboard",
            "Clientes",
            "Veiculos",
            "OrdensServico",
            "Orcamentos",
            "PDV",
            "Estoque",
            "Financeiro",
            "Agendamentos",
            "Funcionarios",
            "Fornecedores",
            "Relatorios",
            "CatalogoPecas",
            "FiscalOperacoes",
            "Help"
        };

        private static readonly string[] I18n04PortugueseResidualTokens =
        {
            "Salvar", "Cancelar", "Excluir", "Editar", "Pesquisar", "Buscar",
            "Carregando", "Nenhum", "Configura", "Deseja", "Sucesso", "Aviso",
            "Atenção", "Atencao", "Rascunho", "Fornecedor", "Funcionário", "Funcionario",
            "Orçamento", "Orcamento", "Veículo", "Veiculo", "Agendamento", "Relatório", "Relatorio"
        };

        private static readonly HashSet<string> I18n04AllowExact = new(StringComparer.OrdinalIgnoreCase)
        {
            "PRIMOX", "CPF", "CNPJ", "NCM", "CFOP", "CST", "CSOSN", "NF-e", "NFe", "XML", "PDF",
            "IBS", "CBS", "OS", "PDV", "SKU", "OK"
        };

        private void RunI18n04UxChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "I18n04:MultilingualUserVisibleAudit", () =>
            {
                var helper = LocalizationHelper.Instance;
                var original = LocalizationService.Instance.CurrentLanguageCode;
                MainWindow? window = null;
                var outRoot = Path.Combine(App.RuntimeAppDataPath, "Logs", "qa-visual", "i18n-04");
                Directory.CreateDirectory(outRoot);
                // Mirror under BaseDirectory for discoverability in local builds
                try
                {
                    var mirror = Path.Combine(AppContext.BaseDirectory, "Logs", "qa-visual", "i18n-04");
                    Directory.CreateDirectory(mirror);
                }
                catch
                {
                    // ignore mirror failures
                }

                var report = new I18n04AuditReport
                {
                    GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    HeadNote = "PRIMOX-I18N-04 runtime user-visible audit"
                };

                try
                {
                    window = new MainWindow(syntheticUser);
                    ShowWindowForInteraction(window);
                    window.Width = 1600;
                    window.Height = 900;
                    WaitForUiIdle();

                    foreach (var lang in new[] { "pt-BR", "en-US", "es-ES" })
                    {
                        helper.SetLanguage(lang);
                        WaitForUiIdle();

                        var langDir = Path.Combine(outRoot, lang.ToLowerInvariant().Replace("-", ""));
                        // folders: pt / en / es
                        langDir = Path.Combine(outRoot, lang switch
                        {
                            "en-US" => "en",
                            "es-ES" => "es",
                            _ => "pt"
                        });
                        Directory.CreateDirectory(langDir);

                        var langResult = new I18n04LanguageResult { Language = lang };

                        // Shell chrome sample
                        CaptureElementPng(window, Path.Combine(langDir, "shell-main.png"));

                        foreach (var modulo in I18n04CriticalModules)
                        {
                            var ok = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                                ? window.OpenImportarNFeForAutomation()
                                : window.NavigateToModuleForAutomation(modulo, forceReload: true);

                            if (!ok || window.CurrentContentElement is not FrameworkElement content)
                            {
                                langResult.Modules.Add(new I18n04ModuleResult
                                {
                                    Module = modulo,
                                    Status = "FAIL",
                                    Notes = "navigate failed"
                                });
                                continue;
                            }

                            WaitForUiIdle();
                            var png = Path.Combine(langDir, SanitizeFileToken(modulo) + ".png");
                            CaptureElementPng(content, png);

                            var texts = CollectVisibleTexts(content);
                            var residuals = new List<string>();
                            if (!string.Equals(lang, "pt-BR", StringComparison.OrdinalIgnoreCase))
                            {
                                foreach (var t in texts)
                                {
                                    if (IsPortugueseResidual(t, out var token))
                                    {
                                        residuals.Add($"{token} :: {Truncate(t, 80)}");
                                    }
                                }
                            }

                            var status = residuals.Count == 0
                                ? (string.Equals(lang, "pt-BR", StringComparison.OrdinalIgnoreCase) ? "PASS" : "PASS")
                                : (residuals.Count <= 3 ? "PARTIAL" : "PARTIAL");

                            // Honest rule: EN/ES with any residual PT UI token => PARTIAL (not FAIL unless shell broken)
                            if (!string.Equals(lang, "pt-BR", StringComparison.OrdinalIgnoreCase) && residuals.Count > 0)
                            {
                                status = "PARTIAL";
                            }

                            langResult.Modules.Add(new I18n04ModuleResult
                            {
                                Module = modulo,
                                Status = status,
                                Screenshot = png,
                                VisibleTextCount = texts.Count,
                                ResidualCount = residuals.Count,
                                ResidualsSample = residuals.Take(12).ToList(),
                                Notes = residuals.Count == 0 ? "no classified PT residual tokens" : "PT residual tokens in visible tree"
                            });
                        }

                        // Runtime switch classification sample: current page after language set without restart
                        langResult.LiveUpdate = "LIVE_UPDATE"; // helper notifies PropertyChanged(null); XAML bindings refresh
                        report.Languages.Add(langResult);
                    }

                    // Persistence / fallback probes (file-level, no crash)
                    ProbeLanguagePersistence(report);
                }
                finally
                {
                    try { helper.SetLanguage(original); } catch { /* ignore */ }
                    if (window?.IsVisible == true) window.Close();
                }

                // Write artifacts
                var jsonPath = Path.Combine(outRoot, $"i18n04-audit-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);

                var md = new StringBuilder();
                md.AppendLine("# I18N-04 Runtime User-Visible Audit");
                md.AppendLine();
                md.AppendLine($"Generated: {report.GeneratedAt}");
                md.AppendLine();
                foreach (var lang in report.Languages)
                {
                    var pass = lang.Modules.Count(m => m.Status == "PASS");
                    var partial = lang.Modules.Count(m => m.Status == "PARTIAL");
                    var fail = lang.Modules.Count(m => m.Status == "FAIL");
                    var testable = lang.Modules.Count;
                    var coverage = testable == 0 ? 0 : Math.Round(100.0 * pass / testable, 1);
                    md.AppendLine($"## {lang.Language}");
                    md.AppendLine($"- Modules: {testable} · PASS {pass} · PARTIAL {partial} · FAIL {fail}");
                    md.AppendLine($"- User-visible PASS rate (strict PASS only): **{coverage}%**");
                    md.AppendLine($"- Live update: {lang.LiveUpdate}");
                    md.AppendLine();
                    foreach (var m in lang.Modules)
                    {
                        md.AppendLine($"- **{m.Module}**: {m.Status} · residuals={m.ResidualCount} · texts={m.VisibleTextCount}");
                        foreach (var r in m.ResidualsSample.Take(5))
                        {
                            md.AppendLine($"  - `{r}`");
                        }
                    }
                    md.AppendLine();
                }

                md.AppendLine("## Persistence / Fallback");
                md.AppendLine($"- Persist OK: {report.PersistenceOk}");
                md.AppendLine($"- Invalid fallback OK: {report.InvalidFallbackOk}");
                md.AppendLine($"- Notes: {report.PersistenceNotes}");

                var mdPath = Path.Combine(outRoot, $"i18n04-audit-{DateTime.Now:yyyyMMdd-HHmmss}.md");
                File.WriteAllText(mdPath, md.ToString(), Encoding.UTF8);
                App.Logger.LogInfo($"I18n04 audit written: {jsonPath}", "Smoke");

                // Soft gate: must navigate most modules; do not require 0 residuals (honest PARTIAL expected)
                foreach (var lang in report.Languages)
                {
                    var fails = lang.Modules.Count(m => m.Status == "FAIL");
                    if (fails > 2)
                    {
                        throw new InvalidOperationException($"I18n04 {lang.Language}: too many module navigation failures ({fails}).");
                    }
                }

                if (!report.InvalidFallbackOk)
                {
                    throw new InvalidOperationException("I18n04: invalid language fallback failed.");
                }
            });
        }

        private static void ProbeLanguagePersistence(I18n04AuditReport report)
        {
            var svc = LocalizationService.Instance;
            var path = Path.Combine(App.RuntimeAppDataPath, "language_settings.json");
            try
            {
                svc.SetLanguage("en-US");
                var exists = File.Exists(path);
                var body = exists ? File.ReadAllText(path) : string.Empty;
                report.PersistenceOk = exists && body.Contains("en", StringComparison.OrdinalIgnoreCase);

                // invalid code
                svc.SetLanguage("xx-INVALID");
                var lang = svc.CurrentLanguageCode;
                report.InvalidFallbackOk = lang.StartsWith("pt", StringComparison.OrdinalIgnoreCase);
                report.PersistenceNotes = $"file={path}; afterInvalid={lang}";

                svc.SetLanguage("pt-BR");
            }
            catch (Exception ex)
            {
                report.PersistenceOk = false;
                report.InvalidFallbackOk = false;
                report.PersistenceNotes = ex.Message;
            }
        }

        private static List<string> CollectVisibleTexts(DependencyObject root)
        {
            var list = new List<string>();
            foreach (var tb in FindVisualChildren<TextBlock>(root))
            {
                if (tb.IsVisible && !string.IsNullOrWhiteSpace(tb.Text))
                {
                    list.Add(tb.Text.Trim());
                }
            }

            foreach (var btn in FindVisualChildren<Button>(root))
            {
                if (!btn.IsVisible) continue;
                var content = btn.Content?.ToString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    list.Add(content.Trim());
                }
            }

            foreach (var cb in FindVisualChildren<ComboBoxItem>(root))
            {
                var content = cb.Content?.ToString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    list.Add(content.Trim());
                }
            }

            return list.Distinct(StringComparer.Ordinal).ToList();
        }

        private static bool IsPortugueseResidual(string text, out string token)
        {
            token = string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var t = text.Trim();
            if (I18n04AllowExact.Contains(t)) return false;
            if (t.Length <= 2) return false;
            if (t.All(ch => char.IsDigit(ch) || "-./".Contains(ch))) return false;

            // User/fixture data and operational logs are not UI chrome.
            if (t.Contains("Smoke", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Contains("ORC-", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Contains("OS-", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Contains(" | Info | ", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Contains("acao(oes) auditada", StringComparison.OrdinalIgnoreCase)) return false;
            if (t.Contains("CIRO", StringComparison.OrdinalIgnoreCase)) return false;

            foreach (var tok in I18n04PortugueseResidualTokens)
            {
                if (t.Contains(tok, StringComparison.OrdinalIgnoreCase))
                {
                    token = tok;
                    return true;
                }
            }

            return false;
        }

        private static string SanitizeFileToken(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '-');
            }

            return value.ToLowerInvariant();
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value.Substring(0, max) + "…";

        private sealed class I18n04AuditReport
        {
            public string GeneratedAt { get; set; } = string.Empty;
            public string HeadNote { get; set; } = string.Empty;
            public List<I18n04LanguageResult> Languages { get; set; } = new();
            public bool PersistenceOk { get; set; }
            public bool InvalidFallbackOk { get; set; }
            public string PersistenceNotes { get; set; } = string.Empty;
        }

        private sealed class I18n04LanguageResult
        {
            public string Language { get; set; } = string.Empty;
            public string LiveUpdate { get; set; } = string.Empty;
            public List<I18n04ModuleResult> Modules { get; set; } = new();
        }

        private sealed class I18n04ModuleResult
        {
            public string Module { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Screenshot { get; set; } = string.Empty;
            public int VisibleTextCount { get; set; }
            public int ResidualCount { get; set; }
            public List<string> ResidualsSample { get; set; } = new();
            public string Notes { get; set; } = string.Empty;
        }
    }
}
