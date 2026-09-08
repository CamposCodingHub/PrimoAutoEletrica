using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Inventario, cobertura e relatorio do PRIMOX QA Engine (Fase 12 funcional).
    /// Nao substitui o Deep QA da Fase 11 — complementa com descoberta + validacao de resultado.
    /// </summary>
    public sealed class PrimoxQaEngine
    {
        public sealed class InventoryItem
        {
            public string Modulo { get; init; } = string.Empty;
            public string Tela { get; init; } = string.Empty;
            public string Controle { get; init; } = string.Empty;
            public string Funcao { get; init; } = string.Empty;
            public string MetodoOuCommand { get; init; } = string.Empty;
            public bool Testavel { get; init; }
            public string Resultado { get; set; } = "DISCOVERED";
        }

        public sealed class CoverageReport
        {
            public int Modulos { get; set; }
            public int Windows { get; set; }
            public int UserControls { get; set; }
            public int BotoesDescobertos { get; set; }
            public int AcoesDescobertas { get; set; }
            public int TestesExecutados { get; set; }
            public int Pass { get; set; }
            public int Fail { get; set; }
            public int Blocked { get; set; }
            public int NotTestable { get; set; }
            public List<InventoryItem> Inventario { get; } = new();
            public List<string> Observacoes { get; } = new();
        }

        public CoverageReport BuildStaticInventory(IReadOnlyList<string> canonicalModules)
        {
            var report = new CoverageReport
            {
                Modulos = canonicalModules.Count
            };

            var assembly = Assembly.GetExecutingAssembly();
            var windows = assembly.GetTypes()
                .Where(t => typeof(Window).IsAssignableFrom(t) && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToList();
            var controls = assembly.GetTypes()
                .Where(t => typeof(UserControl).IsAssignableFrom(t) && !t.IsAbstract)
                .OrderBy(t => t.Name)
                .ToList();

            report.Windows = windows.Count;
            report.UserControls = controls.Count;

            foreach (var module in canonicalModules)
            {
                report.Inventario.Add(new InventoryItem
                {
                    Modulo = module,
                    Tela = module,
                    Controle = "MainWindow.Navigate",
                    Funcao = "Navegar",
                    MetodoOuCommand = "NavigateToModuleForAutomation",
                    Testavel = true,
                    Resultado = "DISCOVERED"
                });
            }

            foreach (var window in windows)
            {
                var methods = window.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name.EndsWith("_Click", StringComparison.Ordinal) ||
                                m.Name.Contains("Salvar", StringComparison.OrdinalIgnoreCase) ||
                                m.Name.Contains("Excluir", StringComparison.OrdinalIgnoreCase) ||
                                m.Name.Contains("Cancelar", StringComparison.OrdinalIgnoreCase))
                    .Select(m => m.Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                foreach (var method in methods)
                {
                    var destructive = method.Contains("Excluir", StringComparison.OrdinalIgnoreCase) ||
                                      method.Contains("Remover", StringComparison.OrdinalIgnoreCase) ||
                                      method.Contains("Apagar", StringComparison.OrdinalIgnoreCase);
                    report.Inventario.Add(new InventoryItem
                    {
                        Modulo = InferModule(window.Name),
                        Tela = window.Name,
                        Controle = window.Name,
                        Funcao = method,
                        MetodoOuCommand = method,
                        Testavel = !destructive,
                        Resultado = destructive ? "NOT TESTABLE (destrutivo — so dialog)" : "DISCOVERED"
                    });
                }
            }

            foreach (var control in controls)
            {
                var methods = control.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name.EndsWith("_Click", StringComparison.Ordinal))
                    .Select(m => m.Name)
                    .Distinct(StringComparer.Ordinal)
                    .Take(40)
                    .ToList();

                foreach (var method in methods)
                {
                    report.Inventario.Add(new InventoryItem
                    {
                        Modulo = InferModule(control.Name),
                        Tela = control.Name,
                        Controle = control.Name,
                        Funcao = method,
                        MetodoOuCommand = method,
                        Testavel = true,
                        Resultado = "DISCOVERED"
                    });
                }
            }

            report.AcoesDescobertas = report.Inventario.Count;
            report.NotTestable = report.Inventario.Count(i => !i.Testavel);
            report.Observacoes.Add("Inventario estatico via reflection de Windows/UserControls/_Click handlers.");
            report.Observacoes.Add("Deep QA Fase 11 permanece ativo e e expandido por este engine.");
            return report;
        }

        public static int CountVisibleButtons(DependencyObject root)
        {
            return FindButtons(root).Count;
        }

        public static IReadOnlyList<Button> FindButtons(DependencyObject root)
        {
            var list = new List<Button>();
            Walk(root, list);
            return list;

            static void Walk(DependencyObject current, List<Button> acc)
            {
                var count = VisualTreeHelper.GetChildrenCount(current);
                for (var i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child is Button button && button.IsVisible)
                    {
                        acc.Add(button);
                    }

                    Walk(child, acc);
                }
            }
        }

        public string WriteReport(CoverageReport report, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            var path = Path.Combine(outputDirectory, $"primox-qa-engine-{DateTime.Now:yyyyMMdd-HHmmss}.md");
            var sb = new StringBuilder();
            sb.AppendLine("# PRIMOX QA Engine — Relatorio de Cobertura");
            sb.AppendLine();
            sb.AppendLine($"Gerado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine("## Resumo");
            sb.AppendLine($"- Modulos: {report.Modulos}");
            sb.AppendLine($"- Windows: {report.Windows}");
            sb.AppendLine($"- UserControls: {report.UserControls}");
            sb.AppendLine($"- Botoes (runtime): {report.BotoesDescobertos}");
            sb.AppendLine($"- Acoes descobertas: {report.AcoesDescobertas}");
            sb.AppendLine($"- Testes executados: {report.TestesExecutados}");
            sb.AppendLine($"- PASS: {report.Pass}");
            sb.AppendLine($"- FAIL: {report.Fail}");
            sb.AppendLine($"- BLOCKED: {report.Blocked}");
            sb.AppendLine($"- NOT TESTABLE: {report.NotTestable}");
            sb.AppendLine();

            var discoveryPct = report.AcoesDescobertas == 0
                ? 0
                : 100.0 * report.Inventario.Count(i => i.Resultado != "DISCOVERED" || i.Testavel) / report.AcoesDescobertas;
            var execPct = report.TestesExecutados == 0
                ? 0
                : 100.0 * report.Pass / report.TestesExecutados;
            sb.AppendLine("## Coberturas");
            sb.AppendLine($"- Descoberta (itens inventariados): {report.AcoesDescobertas}");
            sb.AppendLine($"- Execucao (PASS/total): {execPct:F1}%");
            sb.AppendLine($"- Validacao: exigida em checks QaEngine:* (re-leitura DB/repo apos UI)");
            sb.AppendLine();

            sb.AppendLine("## Observacoes");
            foreach (var obs in report.Observacoes)
            {
                sb.AppendLine($"- {obs}");
            }

            sb.AppendLine();
            sb.AppendLine("## Inventario (amostra)");
            sb.AppendLine("| Modulo | Tela | Funcao | Testavel | Resultado |");
            sb.AppendLine("| ------ | ---- | ------ | -------- | --------- |");
            foreach (var item in report.Inventario.Take(120))
            {
                sb.AppendLine($"| {item.Modulo} | {item.Tela} | {item.Funcao} | {item.Testavel} | {item.Resultado} |");
            }

            if (report.Inventario.Count > 120)
            {
                sb.AppendLine($"| ... | ... | (+{report.Inventario.Count - 120} itens) | ... | ... |");
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        private static string InferModule(string typeName)
        {
            if (typeName.Contains("Cliente", StringComparison.OrdinalIgnoreCase)) return "Clientes";
            if (typeName.Contains("Veiculo", StringComparison.OrdinalIgnoreCase)) return "Veiculos";
            if (typeName.Contains("Funcionario", StringComparison.OrdinalIgnoreCase)) return "Funcionarios";
            if (typeName.Contains("Ordem", StringComparison.OrdinalIgnoreCase) || typeName.Contains("OS", StringComparison.OrdinalIgnoreCase)) return "OrdensServico";
            if (typeName.Contains("Orcamento", StringComparison.OrdinalIgnoreCase)) return "Orcamentos";
            if (typeName.Contains("Agenda", StringComparison.OrdinalIgnoreCase)) return "Agendamentos";
            if (typeName.Contains("Estoque", StringComparison.OrdinalIgnoreCase) || typeName.Contains("Produto", StringComparison.OrdinalIgnoreCase)) return "Estoque";
            if (typeName.Contains("Financeiro", StringComparison.OrdinalIgnoreCase)) return "Financeiro";
            if (typeName.Contains("Relatorio", StringComparison.OrdinalIgnoreCase)) return "Relatorios";
            if (typeName.Contains("PDV", StringComparison.OrdinalIgnoreCase) || typeName.Contains("Venda", StringComparison.OrdinalIgnoreCase)) return "PDV";
            if (typeName.Contains("Fornecedor", StringComparison.OrdinalIgnoreCase)) return "Fornecedores";
            if (typeName.Contains("NFe", StringComparison.OrdinalIgnoreCase)) return "ImportarNFe";
            if (typeName.Contains("Kanban", StringComparison.OrdinalIgnoreCase) || typeName.Contains("Oficina", StringComparison.OrdinalIgnoreCase)) return "OficinaKanban";
            if (typeName.Contains("Catalogo", StringComparison.OrdinalIgnoreCase)) return "CatalogoPecas";
            if (typeName.Contains("Config", StringComparison.OrdinalIgnoreCase)) return "Configuracoes";
            if (typeName.Contains("Help", StringComparison.OrdinalIgnoreCase) || typeName.Contains("Ajuda", StringComparison.OrdinalIgnoreCase)) return "Help";
            if (typeName.Contains("Dashboard", StringComparison.OrdinalIgnoreCase)) return "Dashboard";
            if (typeName.Contains("AutoEletrica", StringComparison.OrdinalIgnoreCase)) return "AutoEletricaTecnica";
            return "Geral";
        }
    }
}
