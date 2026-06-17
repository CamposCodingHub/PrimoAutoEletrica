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
    public sealed partial class UiSmokeTestService
    {
        // Checks de navegacao da shell e regressao do NavigationService.

        private void RunMainWindowNavigationChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunPreCheckSemModaisPresas(result, syntheticUser);

            RunCheck(result, "MainWindow:NavegacaoSequencial", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);

                    if (!window.NavigateToModuleForAutomation("Clientes"))
                    {
                        throw new InvalidOperationException("Falha ao navegar para Clientes.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Clientes", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Modulo atual nao foi atualizado para Clientes.");
                    }

                    if (window.CurrentContentElement is not ClientesControl)
                    {
                        throw new InvalidOperationException("Conteudo atual nao corresponde ao controle de Clientes.");
                    }

                    if (!window.IsModuleHighlightedForAutomation("Clientes"))
                    {
                        throw new InvalidOperationException("Menu de Clientes nao ficou ativo apos a navegacao.");
                    }

                    if (!window.NavigateToModuleForAutomation("Estoque"))
                    {
                        throw new InvalidOperationException("Falha ao navegar para Estoque.");
                    }

                    if (!window.CanNavigateBack)
                    {
                        throw new InvalidOperationException("Historico de navegacao nao foi acumulado.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:VoltarHistorico", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    window.NavigateToModuleForAutomation("Clientes");
                    window.NavigateToModuleForAutomation("Estoque");

                    if (!window.NavigateBackForAutomation())
                    {
                        throw new InvalidOperationException("Falha ao voltar para o modulo anterior.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Clientes", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Volta de historico nao retornou para Clientes.");
                    }

                    if (window.CurrentContentElement is not ClientesControl)
                    {
                        throw new InvalidOperationException("Conteudo apos voltar nao corresponde ao controle esperado.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:AtualizarModulo", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    window.NavigateToModuleForAutomation("Clientes");
                    var firstInstance = window.CurrentContentElement
                        ?? throw new InvalidOperationException("Clientes nao foi carregado para o teste de refresh.");

                    if (!window.RefreshCurrentModuleForAutomation())
                    {
                        throw new InvalidOperationException("Refresh do modulo atual falhou.");
                    }

                    var refreshedInstance = window.CurrentContentElement
                        ?? throw new InvalidOperationException("Modulo nao permaneceu carregado apos o refresh.");

                    if (ReferenceEquals(firstInstance, refreshedInstance))
                    {
                        throw new InvalidOperationException("Refresh do modulo reutilizou a mesma instancia visual.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:PermissoesMenuVendedor", () =>
            {
                var vendedor = CreateSyntheticUser("Vendedor", "Smoke Vendedor");
                var window = new MainWindow(vendedor);

                try
                {
                    InitializeWindowForInteraction(window);

                    if (!window.IsMenuEnabledForAutomation("Clientes"))
                    {
                        throw new InvalidOperationException("Menu de Clientes deveria estar habilitado para vendedor.");
                    }

                    if (window.IsMenuEnabledForAutomation("Financeiro"))
                    {
                        throw new InvalidOperationException("Menu Financeiro nao deveria estar habilitado para vendedor.");
                    }

                    if (window.IsMenuEnabledForAutomation("PDV"))
                    {
                        throw new InvalidOperationException("Menu PDV nao deveria estar habilitado para vendedor.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:BuscaGlobalNavegaModulo", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);

                    var searchResult = new SearchResult
                    {
                        Titulo = "Estoque",
                        Subtitulo = "Modulo operacional de estoque",
                        Tipo = "Modulo",
                        Acao = "Estoque"
                    };

                    if (!window.NavigateFromSearchResultForAutomation(searchResult))
                    {
                        throw new InvalidOperationException("Busca global nao conseguiu navegar para o modulo esperado.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Estoque", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Busca global nao atualizou o modulo atual.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:ImportarNFeAutomacao", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);

                    if (!window.OpenImportarNFeForAutomation())
                    {
                        throw new InvalidOperationException("A navegacao automatizada para a pagina de Importar NF-e falhou.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "ImportarNFe", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A pagina de Importar NF-e nao ficou ativa apos a navegacao automatizada.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:BuscaGlobalSobDemanda", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);

                    if (window.IsGlobalSearchPrimedForAutomation())
                    {
                        throw new InvalidOperationException("A busca global nao deveria carregar o indice completo no startup.");
                    }

                    window.EnsureGlobalSearchReadyForAutomation();

                    if (!window.IsGlobalSearchPrimedForAutomation())
                    {
                        throw new InvalidOperationException("A busca global nao carregou o indice sob demanda.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:NotificacaoShellAcionavel", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    ShellNotificationService.PublishNavigationHint(
                        title: "Atalho de shell",
                        message: "Abrir Financeiro a partir do banner do shell.",
                        actionModule: "Financeiro",
                        actionLabel: "Abrir Financeiro",
                        source: "SmokeTest");

                    if (!window.HasShellNotificationForAutomation("Financeiro"))
                    {
                        throw new InvalidOperationException("O shell nao exibiu a notificacao acionavel esperada.");
                    }

                    if (!window.InvokeShellNotificationActionForAutomation())
                    {
                        throw new InvalidOperationException("A acao da notificacao do shell falhou.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Financeiro", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A notificacao do shell nao navegou para o modulo Financeiro.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:CruzadaAgendamentosParaOS", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    if (!window.NavigateToModuleForAutomation("Agendamentos"))
                    {
                        throw new InvalidOperationException("Falha ao navegar para Agendamentos.");
                    }

                    if (window.CurrentContentElement is not AgendamentosControl agendamentosControl)
                    {
                        throw new InvalidOperationException("O conteudo atual nao corresponde ao controle de Agendamentos.");
                    }

                    agendamentosControl.EmitirSugestaoOrdensServicoForAutomation();
                    if (!window.HasShellNotificationForAutomation("Ordens de Servico"))
                    {
                        throw new InvalidOperationException("A navegacao cruzada de Agendamentos nao apareceu no shell.");
                    }

                    if (!window.InvokeShellNotificationActionForAutomation())
                    {
                        throw new InvalidOperationException("A acao do shell para Ordens de Servico falhou.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "OrdensServico", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A acao cruzada nao levou ao modulo de Ordens de Servico.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });

            RunCheck(result, "MainWindow:CruzadaOrcamentosParaFinanceiro", () =>
            {
                var window = new MainWindow(syntheticUser);

                try
                {
                    InitializeWindowForInteraction(window);
                    if (!window.NavigateToModuleForAutomation("Orcamentos"))
                    {
                        throw new InvalidOperationException("Falha ao navegar para Orcamentos.");
                    }

                    if (window.CurrentContentElement is not OrcamentosControl orcamentosControl)
                    {
                        throw new InvalidOperationException("O conteudo atual nao corresponde ao controle de Orcamentos.");
                    }

                    orcamentosControl.EmitirSugestaoFinanceiroForAutomation();
                    if (!window.HasShellNotificationForAutomation("Financeiro"))
                    {
                        throw new InvalidOperationException("A navegacao cruzada de Orcamentos nao apareceu no shell.");
                    }

                    if (!window.InvokeShellNotificationActionForAutomation())
                    {
                        throw new InvalidOperationException("A acao do shell para Financeiro falhou.");
                    }

                    if (!string.Equals(window.CurrentModuleName, "Financeiro", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("A acao cruzada nao levou ao modulo Financeiro.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private void RunPreCheckSemModaisPresas(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "PreCheck:SemModaisPresasNavegacao", () =>
            {
                var window = new MainWindow(syntheticUser);
                var modulos = CoreModules
                    .Concat(new[] { "ImportarNFe", "Configuracoes" })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                try
                {
                    InitializeWindowForInteraction(window);
                    window.Show();
                    WaitForUiIdle();

                    foreach (var modulo in modulos)
                    {
                        var sucesso = string.Equals(modulo, "ImportarNFe", StringComparison.OrdinalIgnoreCase)
                            ? window.OpenImportarNFeForAutomation()
                            : string.Equals(modulo, "Configuracoes", StringComparison.OrdinalIgnoreCase)
                                ? window.OpenConfiguracoesForAutomation()
                                : window.NavigateToModuleForAutomation(modulo, forceReload: true);

                        if (!sucesso)
                        {
                            throw new InvalidOperationException($"Falha ao navegar para {modulo} no pre-check de modais.");
                        }

                        WaitForUiIdle();
                        var modaisVisiveis = ObterJanelasTransientesVisiveis(window);
                        if (modaisVisiveis.Count > 0)
                        {
                            throw new InvalidOperationException(
                                $"Modal/janela transiente permaneceu aberta apos navegar para {modulo}: {string.Join(", ", modaisVisiveis)}.");
                        }
                    }
                }
                finally
                {
                    CloseTransientWindows(window);
                    window.Close();
                }
            });
        }

        private void RunNavigationServiceRegressionChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "NavigationService:ModuloInexistente", () =>
            {
                var permissionService = new PermissionService(syntheticUser, _logger, App.Database);
                var navigationService = new NavigationService(permissionService, _logger);

                var control = navigationService.Navigate("ModuloInexistente");
                if (control != null)
                {
                    throw new InvalidOperationException("Servico de navegacao criou controle para um modulo inexistente.");
                }
            });

            RunCheck(result, "NavigationService:RefreshSemModulo", () =>
            {
                var permissionService = new PermissionService(syntheticUser, _logger, App.Database);
                var navigationService = new NavigationService(permissionService, _logger);

                var control = navigationService.RefreshCurrent();
                if (control != null)
                {
                    throw new InvalidOperationException("Refresh sem modulo atual deveria retornar nulo.");
                }
            });

            RunCheck(result, "NavigationService:PermissaoNegada", () =>
            {
                var vendedor = CreateSyntheticUser("Vendedor", "Vendedor Smoke");
                var permissionService = new PermissionService(vendedor, _logger, App.Database);
                var navigationService = new NavigationService(permissionService, _logger);

                var control = navigationService.Navigate("Financeiro");
                if (control != null)
                {
                    throw new InvalidOperationException("O servico de navegacao permitiu acesso indevido ao modulo Financeiro.");
                }
            });
        }

    }
}
