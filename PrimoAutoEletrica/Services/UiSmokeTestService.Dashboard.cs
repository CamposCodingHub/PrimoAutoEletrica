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

                    WaitForCondition(
                        () => dashboard.Metrics.Count >= 11 && dashboard.RevenueBars.Count == 7,
                        TimeSpan.FromSeconds(5),
                        "Dashboard nao carregou metricas e grafico operacional.");

                    var titulosObrigatorios = new[]
                    {
                        "Faturamento do dia",
                        "Faturamento do mes",
                        "OS abertas",
                        "OS atrasadas",
                        "Orcamentos pendentes",
                        "Veiculos na oficina",
                        "Contas a receber",
                        "Contas a pagar",
                        "Estoque critico",
                        "Agenda do dia",
                        "Servicos em andamento"
                    };

                    foreach (var titulo in titulosObrigatorios)
                    {
                        if (!dashboard.Metrics.Any(metric => string.Equals(metric.Titulo, titulo, StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new InvalidOperationException($"Dashboard nao exibiu o card obrigatorio '{titulo}'.");
                        }
                    }

                    var faturamentoDia = dashboard.Metrics.First(metric => metric.Titulo == "Faturamento do dia");
                    if (string.Equals(faturamentoDia.Valor, "R$ 0,00", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Dashboard mostrou faturamento zerado mesmo apos venda real de smoke.");
                    }

                    if (dashboard.Highlights.Count < 3)
                    {
                        throw new InvalidOperationException("Dashboard nao exibiu resumo operacional minimo.");
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
