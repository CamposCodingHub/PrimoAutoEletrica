using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using System;
using System.Linq;

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

                    // Contrato atual do DashboardViewModel (nao inventar KPIs so para o teste):
                    // metricas base: Faturamento do mes, OS abertas, Orcamentos pendentes, Clientes, Produtos em estoque
                    // (+ Estoque baixo opcional). Barras: ate 7 dias (podem ser menos).
                    WaitForCondition(
                        () => dashboard.Metrics.Count >= 5,
                        TimeSpan.FromSeconds(8),
                        "Dashboard nao carregou metricas operacionais.");

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
                            throw new InvalidOperationException($"Dashboard nao exibiu o card obrigatorio '{titulo}'.");
                        }
                    }

                    var faturamentoMes = dashboard.Metrics.First(metric => metric.Titulo == "Faturamento do mes");
                    if (string.IsNullOrWhiteSpace(faturamentoMes.Valor))
                    {
                        throw new InvalidOperationException("Dashboard nao exibiu valor de faturamento do mes.");
                    }

                    if (dashboard.Highlights.Count < 1)
                    {
                        throw new InvalidOperationException("Dashboard nao exibiu nenhum destaque operacional.");
                    }

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
