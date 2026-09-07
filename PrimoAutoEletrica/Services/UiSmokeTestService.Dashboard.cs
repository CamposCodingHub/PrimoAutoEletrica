using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using System;
using System.Linq;
using System.Windows;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private void RunDashboardOperationalChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Dashboard:IndicadoresReaisAtalhos", () =>
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);

                var venda = CreateSyntheticVenda(_fixture.Cliente, _fixture.Produto, syntheticUser.Nome);
                new VendaService(App.Database).RegistrarVenda(venda, atualizarEstoque: false);

                var window = new MainWindow(syntheticUser);
                try
                {
                    ShowWindowForInteraction(window);

                    if (window.CurrentContentElement is not DashboardControl dashboard)
                    {
                        if (!window.NavigateToModuleForAutomation("Dashboard", forceReload: true) ||
                            window.CurrentContentElement is not DashboardControl dashboardReloaded)
                        {
                            throw new InvalidOperationException("Dashboard nao foi carregado pelo shell.");
                        }

                        dashboard = dashboardReloaded;
                    }

                    WaitForCondition(
                        () => dashboard.Metrics.Count >= 5,
                        TimeSpan.FromSeconds(10),
                        "Centro de Operacoes nao carregou metricas operacionais.");

                    var titulosObrigatorios = new[]
                    {
                        "Faturamento do mes",
                        "OS abertas",
                        "Orcamentos pendentes",
                        "Clientes",
                        "Produtos em estoque"
                    };

                    foreach (var titulo in titulosObrigatorios)
                    {
                        if (!dashboard.Metrics.Any(metric => string.Equals(metric.Titulo, titulo, StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new InvalidOperationException($"Workshop Pulse nao exibiu o indicador '{titulo}'.");
                        }
                    }

                    var faturamentoMes = dashboard.Metrics.First(metric => metric.Titulo == "Faturamento do mes");
                    if (string.IsNullOrWhiteSpace(faturamentoMes.Valor))
                    {
                        throw new InvalidOperationException("Dashboard nao exibiu valor de faturamento do mes.");
                    }

                    if (dashboard.FlowStages.Count < 5)
                    {
                        throw new InvalidOperationException("Fluxo operacional nao carregou os estagios reais do Kanban.");
                    }

                    if (dashboard.Highlights.Count < 1 && !dashboard.IsAttentionEmpty && dashboard.AttentionItems.Count == 0)
                    {
                        throw new InvalidOperationException("Attention Center sem estado vazio nem itens.");
                    }

                    // Viewport critico 1366x768 — shell + dashboard sem crash
                    window.Width = 1366;
                    window.Height = 768;
                    window.WindowState = WindowState.Normal;
                    window.UpdateLayout();
                    WaitForUiIdle();

                    ClickButton(dashboard, "AtalhoFinanceiroDashboardButton");
                    WaitForCondition(
                        () => window.CurrentContentElement is FinanceiroControl,
                        TimeSpan.FromSeconds(5),
                        "Atalho do Dashboard para Financeiro nao navegou pelo shell.");
                }
                finally
                {
                    if (window.IsVisible)
                    {
                        window.Close();
                    }
                }
            });
        }
    }
}
