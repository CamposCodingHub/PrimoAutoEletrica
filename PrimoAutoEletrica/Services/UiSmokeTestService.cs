using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using PdfSharpCore.Pdf.IO;
using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Views.Clientes;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Executa uma validacao leve da camada de UI para garantir que os modulos
    /// principais possam ser construidos e preparados sem derrubar a aplicacao.
    /// </summary>
    public sealed partial class UiSmokeTestService
    {
        private static readonly Type[] CoreControlTypes =
        {
            typeof(DashboardControl),
            typeof(ClientesControl),
            typeof(VeiculosControl),
            typeof(AutoEletricaTecnicaControl),
            typeof(OrcamentosControl),
            typeof(OrdensServicoControl),
            typeof(OficinaKanbanControl),
            typeof(PDVControl),
            typeof(EstoqueControl),
            typeof(CatalogoPecasControl),
            typeof(ImportarNFeControl),
            typeof(FinanceiroControl),
            typeof(FornecedoresControl),
            typeof(FuncionariosControl),
            typeof(AgendamentosControl),
            typeof(RelatoriosControl)
        };

        private static readonly string[] CoreModules =
        {
            "Dashboard",
            "Clientes",
            "Veiculos",
            "AutoEletricaTecnica",
            "Orcamentos",
            "OrdensServico",
            "OficinaKanban",
            "PDV",
            "Estoque",
            "CatalogoPecas",
            "ImportarNFe",
            "Financeiro",
            "Fornecedores",
            "Funcionarios",
            "Agendamentos",
            "Relatorios"
        };

        private readonly LoggerService _logger;
        private readonly string _checkFilter;
        private UiSmokeFixture? _fixture;

        public UiSmokeTestService(LoggerService logger, string? checkFilter = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _checkFilter = checkFilter?.Trim() ?? string.Empty;
        }

        public UiSmokeTestRunResult Run()
        {
            var result = new UiSmokeTestRunResult();
            var syntheticUser = EnsureFuncionario("smoke-admin@primoauto.com", "Smoke Test", "Administrador");

            var inicioDetalhe = string.IsNullOrWhiteSpace(_checkFilter)
                ? "Inicio da validacao automatizada da interface."
                : $"Inicio da validacao automatizada da interface com filtro '{_checkFilter}'.";
            _logger.LogInfo("Iniciando smoke test de UI.");
            App.Audit.RegistrarSistema("SmokeTestUi", inicioDetalhe);

            App.Session.StartSession(syntheticUser);

            try
            {
                if (!string.IsNullOrWhiteSpace(_checkFilter))
                {
                    RunFilteredChecks(result, syntheticUser);
                    return FinalizarSmokeTest(result);
                }

                RunCheck(result, "MainWindow", () =>
                {
                    var window = new MainWindow(syntheticUser);
                    PrepareWindow(window);
                });

                RunCheck(result, "Dados:SeedBaseUi", () =>
                {
                    _fixture = EnsureSmokeFixture(syntheticUser);
                });

                RunProdutosCamposAnexosChecks(result);
                RunProdutosCadastroCompletoChecks(result);
                RunProdutosEtiquetaPdfChecks(result);
                RunEstoqueOperationalChecks(result);
                RunFornecedoresProdutoFornecedorChecks(result);
                RunFornecedoresSegurancaExclusaoChecks(result);
                RunFornecedoresEdicaoFichaChecks(result);
                RunClientesLgpdChecks(result);
                RunClientesCadastroCompletoChecks(result);
                RunClientesAnexosAssinaturaChecks(result);
                RunDashboardOperationalChecks(result, syntheticUser);
                RunVeiculosCadastroCompletoChecks(result);
                RunVeiculosAlertasMidiaChecks(result);
                RunAutoEletricaTecnicaChecks(result, syntheticUser);
                RunOrdensServicoMidiasChecklistFinanceiroChecks(result);
                RunOficinaKanbanChecks(result);
                RunOrcamentosConversoesPdfWhatsAppAlertasChecks(result);
                RunDocumentosPdfImpressaoExportacaoChecks(result);
                RunDocumentacaoEntregaChecks(result);
                RunAgendamentosVisualizacoesConversoesChecks(result);
                RunFinanceiroGraficosAlertasChecks(result);
                RunImportarNFeRollbackChecks(result, syntheticUser);
                RunImportarNFeXmlRealRelancamentoChecks(result, syntheticUser);
                RunFuncionariosPermissoesAuditoriaChecks(result, syntheticUser);
                RunConfiguracoesComerciaisBackupChecks(result, syntheticUser);
                RunRobustezErrosLogsChecks(result);
                RunTemaModulosChecks(result, syntheticUser);
                RunLoginSessaoSegurancaChecks(result, syntheticUser);

                RunMainWindowNavigationChecks(result, syntheticUser);

                var permissionService = new PermissionService(syntheticUser, _logger, App.Database);
                var navigationService = new NavigationService(permissionService, _logger);

                foreach (var module in CoreModules)
                {
                    RunCheck(result, $"Modulo:{module}", () =>
                    {
                        var control = navigationService.Navigate(module)
                            ?? throw new InvalidOperationException($"NavigationService retornou nulo para o modulo {module}.");

                        PrepareElement(control);
                    });
                }

                RunCheck(result, "Janela:ImportarNota", () =>
                {
                    var window = new ImportarNotaWindow();
                    PrepareWindow(window);
                });

                RunCheck(result, "Janela:ImportarCatalogoPecas", () =>
                {
                    var window = new ImportarCatalogoPecasWindow();
                    PrepareWindow(window);
                });

                RunDiscoveredWindows(result);
                RunDiscoveredUserControls(result);
                RunInteractiveMainWindowChecks(result, syntheticUser);
                RunPdvOperationalInteractionChecks(result);
                RunInteractiveCoreControlChecks(result);
                RunParameterizedWindows(result, syntheticUser);
                RunInteractiveWindowButtonChecks(result, syntheticUser);
                RunInteractiveDiscoveredUserControlChecks(result);
                RunRelatoriosOperationalChecks(result);
                RunNavigationServiceRegressionChecks(result, syntheticUser);
            }
            finally
            {
                App.Session.EndSession();
            }

            return FinalizarSmokeTest(result);
        }

        private UiSmokeTestRunResult FinalizarSmokeTest(UiSmokeTestRunResult result)
        {
            result.ReportPath = PersistReport(result);
            var summary = $"Smoke test UI concluido. Total={result.TotalChecks}, Sucesso={result.PassedChecks}, Falhas={result.FailedChecks}.";

            if (result.HasFailures)
            {
                _logger.LogError(summary);
                App.Audit.RegistrarSistema("SmokeTestUi", summary, "Error", false);
            }
            else
            {
                _logger.LogInfo(summary);
                App.Audit.RegistrarSistema("SmokeTestUi", summary);
            }

            return result;
        }

        private void RunFilteredChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            if (FiltroCombina("MainWindow"))
            {
                RunCheck(result, "MainWindow", () =>
                {
                    var window = new MainWindow(syntheticUser);
                    PrepareWindow(window);
                });
            }

            if (FiltroCombina("Dados:SeedBaseUi"))
            {
                RunCheck(result, "Dados:SeedBaseUi", () =>
                {
                    _fixture = EnsureSmokeFixture(syntheticUser);
                });
            }

            if (FiltroCombina("PreCheck") || FiltroCombina("SemModaisPresas"))
            {
                RunPreCheckSemModaisPresas(result, syntheticUser);
            }

            if (FiltroCombina("Dashboard"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunDashboardOperationalChecks(result, syntheticUser);
            }

            if (FiltroCombina("ImportarNFe"))
            {
                RunImportarNFeRollbackChecks(result, syntheticUser);
                RunImportarNFeXmlRealRelancamentoChecks(result, syntheticUser);
            }

            if (FiltroCombina("PDV"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunPdvOperationalInteractionChecks(result);
            }

            if (FiltroCombina("Fornecedores"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunFornecedoresProdutoFornecedorChecks(result);
                RunFornecedoresSegurancaExclusaoChecks(result);
                RunFornecedoresEdicaoFichaChecks(result);
            }

            if (FiltroCombina("Produtos"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunProdutosCamposAnexosChecks(result);
                RunProdutosCadastroCompletoChecks(result);
                RunProdutosEtiquetaPdfChecks(result);
            }

            if (FiltroCombina("Estoque"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunEstoqueOperationalChecks(result);
            }

            if (FiltroCombina("Clientes"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunClientesLgpdChecks(result);
                RunClientesCadastroCompletoChecks(result);
                RunClientesAnexosAssinaturaChecks(result);
            }

            if (FiltroCombina("Veiculos"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunVeiculosCadastroCompletoChecks(result);
                RunVeiculosAlertasMidiaChecks(result);
            }

            if (FiltroCombina("AutoEletrica") || FiltroCombina("AutoEletricaTecnica") || FiltroCombina("Tecnica"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunAutoEletricaTecnicaChecks(result, syntheticUser);
            }

            if (FiltroCombina("OrdensServico"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunOrdensServicoMidiasChecklistFinanceiroChecks(result);
            }

            if (FiltroCombina("OficinaKanban") || FiltroCombina("Kanban"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunOficinaKanbanChecks(result);
            }

            if (FiltroCombina("Orcamentos"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunOrcamentosConversoesPdfWhatsAppAlertasChecks(result);
            }

            if (FiltroCombina("Documentos") || FiltroCombina("PDF") || FiltroCombina("Impressao") || FiltroCombina("Exportacao"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunDocumentosPdfImpressaoExportacaoChecks(result);
            }

            if (FiltroCombina("Documentacao") || FiltroCombina("Manual") || FiltroCombina("Roadmap"))
            {
                RunDocumentacaoEntregaChecks(result);
            }

            if (FiltroCombina("Agendamentos"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunAgendamentosVisualizacoesConversoesChecks(result);
            }

            if (FiltroCombina("Calendar") || FiltroCombina("Calendario"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunCalendarInteractionChecks(result, syntheticUser);
            }

            if (FiltroCombina("Financeiro"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunFinanceiroGraficosAlertasChecks(result);
            }

            if (FiltroCombina("Relatorios"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunRelatoriosOperationalChecks(result);
            }

            if (FiltroCombina("Funcionarios"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunFuncionariosPermissoesAuditoriaChecks(result, syntheticUser);
            }

            if (FiltroCombina("Configuracoes"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunConfiguracoesComerciaisBackupChecks(result, syntheticUser);
            }

            if (FiltroCombina("Robustez") || FiltroCombina("Logs") || FiltroCombina("Erro"))
            {
                RunRobustezErrosLogsChecks(result);
            }

            if (FiltroCombina("Tema"))
            {
                RunTemaModulosChecks(result, syntheticUser);
            }

            // Primox QA Engine (funcional/persistencia). Nao usar filtro bare "Qa" — conflitaria com DeepQa.
            if (FiltroCombina("QaEngine") || FiltroCombina("FunctionalQa") || FiltroCombina("PrimoxQa"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunPrimoxQaEngineChecks(result, syntheticUser);
            }

            if (FiltroCombina("CompleteUi") || FiltroCombina("Fase15E") || FiltroCombina("QaEngine"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunPrimoxCompleteUiChecks(result, syntheticUser);
            }

            if (FiltroCombina("DeepQa") || FiltroCombina("LongRun") || FiltroCombina("DeepQA"))
            {
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunDeepQaChecks(result, syntheticUser);
            }

            if (FiltroCombina("Sidebar") || FiltroCombina("Shell"))
            {
                RunSidebarShellChecks(result, syntheticUser);
            }

            if (FiltroCombina("CommandCenter") || FiltroCombina("CommandPalette") || FiltroCombina("Paleta"))
            {
                RunCommandCenterShellChecks(result, syntheticUser);
            }

            if (FiltroCombina("Components") || FiltroCombina("Componentes") || FiltroCombina("GlobalComponents"))
            {
                RunGlobalComponentsChecks(result, syntheticUser);
            }

            if (FiltroCombina("LoginSessao"))
            {
                RunLoginSessaoSegurancaChecks(result, syntheticUser);
            }

            if (result.TotalChecks == 0)
            {
                RunCheck(result, $"Filtro:{_checkFilter}", () =>
                {
                    throw new InvalidOperationException($"Nenhum smoke check conhecido combina com o filtro '{_checkFilter}'.");
                });
            }
        }

        private bool FiltroCombina(string valor)
        {
            return valor.Contains(_checkFilter, StringComparison.OrdinalIgnoreCase) ||
                   _checkFilter.Contains(valor, StringComparison.OrdinalIgnoreCase);
        }

        private void RunCheck(UiSmokeTestRunResult result, string name, Action action)
        {
            if (!DeveExecutarCheck(name))
            {
                return;
            }

            var timer = Stopwatch.StartNew();
            _logger.LogInfo($"Smoke test iniciando: {name}.");

            try
            {
                action();
                timer.Stop();

                const int warnThresholdMs = 30000; // 30s
                // DeepQa/long-run intencionalmente ultrapassa 2 min; demais checks mantem o limite padrao.
                var failThresholdMs = name.StartsWith("DeepQa:", StringComparison.OrdinalIgnoreCase)
                    ? 600000
                    : name.StartsWith("QaEngine:", StringComparison.OrdinalIgnoreCase)
                        ? 600000
                    : name.StartsWith("Tema:", StringComparison.OrdinalIgnoreCase)
                        ? 300000
                        : 120000;

                if (timer.ElapsedMilliseconds > warnThresholdMs)
                {
                    try { _logger.LogWarning($"Smoke test '{name}' demorou {timer.ElapsedMilliseconds} ms (acima de {warnThresholdMs} ms)."); } catch { }
                }

                if (timer.ElapsedMilliseconds > failThresholdMs)
                {
                    throw new TimeoutException($"Smoke test '{name}' excedeu o tempo limite de {failThresholdMs} ms.");
                }

                result.Checks.Add(new UiSmokeTestCheckResult
                {
                    Name = name,
                    Success = true,
                    DurationMs = timer.ElapsedMilliseconds,
                    Message = "OK"
                });

                _logger.LogInfo($"Smoke test aprovado: {name} ({timer.ElapsedMilliseconds} ms).");
            }
            catch (Exception ex)
            {
                timer.Stop();

                result.Checks.Add(new UiSmokeTestCheckResult
                {
                    Name = name,
                    Success = false,
                    DurationMs = timer.ElapsedMilliseconds,
                    Message = ex.Message
                });

                _logger.LogError($"Smoke test falhou: {name}", ex);
            }
        }

        private bool DeveExecutarCheck(string name)
        {
            return string.IsNullOrWhiteSpace(_checkFilter) ||
                   name.Contains(_checkFilter, StringComparison.OrdinalIgnoreCase);
        }

    }

    public sealed class UiSmokeTestRunResult
    {
        public List<UiSmokeTestCheckResult> Checks { get; } = new();
        public string ReportPath { get; set; } = string.Empty;
        public int TotalChecks => Checks.Count;
        public int PassedChecks => Checks.Count(check => check.Success);
        public int FailedChecks => Checks.Count(check => !check.Success);
        public bool HasFailures => FailedChecks > 0;
    }

    public sealed class UiSmokeTestCheckResult
    {
        public string Name { get; set; } = string.Empty;
        public bool Success { get; set; }
        public long DurationMs { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
