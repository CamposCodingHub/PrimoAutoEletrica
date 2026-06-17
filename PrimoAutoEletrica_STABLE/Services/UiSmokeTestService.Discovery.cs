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
        // Descoberta automatica de janelas, controles e superficies parametrizadas.

        private void RunDiscoveredWindows(UiSmokeTestRunResult result)
        {
            var excludedTypes = new HashSet<Type>
            {
                typeof(MainWindow),
                typeof(ImportarNotaWindow),
                typeof(ConfigurarPermissoesWindow),
                typeof(GerenciarPerfisWindow),
                typeof(EditarFornecedorWindow),
                typeof(EditarFuncionarioWindow),
                typeof(EditarClienteWindow),
                typeof(EditarProdutoWindow),
                typeof(HistoricoClienteWindow),
                typeof(NovoFuncionarioWindow),
                typeof(NovoOrcamentoWindow),
                typeof(NovoPerfilWindow),
                typeof(NovoVeiculoWindow),
                typeof(OrdemServicoWindow),
                typeof(SelecionarOrcamentoWindow),
                typeof(VisualizarFornecedorWindow),
                typeof(VisualizarVeiculoWindow),
                typeof(AdicionarFornecedorDialog)
            };

            var windowTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(Window).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null &&
                    !excludedTypes.Contains(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            foreach (var windowType in windowTypes)
            {
                RunCheck(result, $"Janela:{windowType.Name}", () =>
                {
                    if (Activator.CreateInstance(windowType) is not Window window)
                    {
                        throw new InvalidOperationException($"Falha ao instanciar a janela {windowType.FullName}.");
                    }

                    PrepareWindow(window);
                });
            }
        }

        private void RunDiscoveredUserControls(UiSmokeTestRunResult result)
        {
            var excludedTypes = new HashSet<Type>(CoreControlTypes);

            var controlTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(UserControl).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null &&
                    !excludedTypes.Contains(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            foreach (var controlType in controlTypes)
            {
                RunCheck(result, $"Controle:{controlType.Name}", () =>
                {
                    if (Activator.CreateInstance(controlType) is not FrameworkElement control)
                    {
                        throw new InvalidOperationException($"Falha ao instanciar o controle {controlType.FullName}.");
                    }

                    PrepareElement(control);
                });
            }
        }

        private void RunParameterizedWindows(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            foreach (var factory in BuildParameterizedWindowFactories(syntheticUser))
            {
                RunCheck(result, factory.Name, () =>
                {
                    var window = factory.Factory();
                    PrepareWindow(window);
                });
            }
        }

        private List<(string Name, Func<Window> Factory)> BuildParameterizedWindowFactories(Funcionario syntheticUser)
        {
            var fixture = _fixture ?? throw new InvalidOperationException("A base sintetica do smoke test ainda nao foi inicializada.");
            fixture.Produto.QuantidadeReservada = Math.Max(fixture.Produto.QuantidadeReservada, 2);
            fixture.Fornecedor = App.Repositories.Fornecedores.ObterPorId(fixture.Fornecedor.Id)
                ?? CreatePersistedFornecedor();

            var sampleHistoricoEstoque = new List<DadoAuditoria>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    DataHora = DateTime.Now.AddMinutes(-20),
                    Usuario = syntheticUser.Nome,
                    Acao = "ReservaAgendamento",
                    Tabela = "Produto",
                    RegistroId = fixture.Produto.Id,
                    Categoria = "Estoque",
                    Severidade = "Info",
                    Sucesso = true,
                    ValorAnterior = "Estoque=12; Reservado=0; Disponivel=12",
                    ValorNovo = "Estoque=12; Reservado=2; Disponivel=10"
                }
            };

            return new List<(string Name, Func<Window> Factory)>
            {
                ("Janela:ConfigurarPermissoesWindow", () => new ConfigurarPermissoesWindow(syntheticUser)),
                ("Janela:GerenciarPerfisWindow", () => new GerenciarPerfisWindow(syntheticUser)),
                ("Janela:NovoFuncionarioWindow", () => new NovoFuncionarioWindow(syntheticUser)),
                ("Janela:NovoPerfilWindow", () => new NovoPerfilWindow(syntheticUser)),
                ("Janela:EditarFuncionarioWindow", () => new EditarFuncionarioWindow(syntheticUser, fixture.Funcionario)),
                ("Janela:EditarClienteWindow", () => new EditarClienteWindow(fixture.Cliente)),
                ("Janela:EditarProdutoWindow", () => new EditarProdutoWindow(fixture.Produto)),
                ("Janela:EditarFornecedorWindow", () => new EditarFornecedorWindow(fixture.Fornecedor)),
                ("Janela:HistoricoClienteWindow", () => new HistoricoClienteWindow(fixture.Cliente)),
                ("Janela:VisualizarFornecedorWindow", () => new VisualizarFornecedorWindow(fixture.Fornecedor)),
                ("Janela:VisualizarVeiculoWindow", () => new VisualizarVeiculoWindow(fixture.Veiculo, App.Database)),
                ("Janela:NovoVeiculoWindow", () => new NovoVeiculoWindow(App.Database, new Dictionary<Guid, Cliente>
                {
                    [fixture.Cliente.Id] = fixture.Cliente
                }, fixture.Veiculo, fixture.Cliente)),
                ("Janela:HistoricoEstoqueWindow", () => new HistoricoEstoqueWindow(fixture.Produto, sampleHistoricoEstoque)),
                ("Janela:OrdemServicoWindow", () => new OrdemServicoWindow(App.Database, null, null)),
                ("Janela:OperacaoCaixaWindow", () => new OperacaoCaixaWindow(new OperacaoCaixaRequest
                {
                    WindowTitle = "Operacao de caixa",
                    Header = "Fluxo visual padronizado",
                    Subheader = "Janela parametrizada usada pelo smoke test para validar o design system global.",
                    ValorLabel = "Valor operacional",
                    ObservacoesObrigatorias = true
                })),
                ("Janela:SelecionarOrcamentoWindow", () => new SelecionarOrcamentoWindow(new List<Orcamento> { fixture.Orcamento })),
                ("Janela:SelecionarVendaWindow", () => new SelecionarVendaWindow(new List<Venda> { fixture.Venda })),
                ("Janela:AdicionarFornecedorDialog", () => new AdicionarFornecedorDialog(fixture.Fornecedor.NomeFantasia))
            };
        }

        private void RunInteractiveMainWindowChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "Interacao:MainWindow:Botoes", () =>
            {
                ExerciseWindowButtons(() => new MainWindow(syntheticUser), typeof(MainWindow));
            });
        }

        private void RunInteractiveCoreControlChecks(UiSmokeTestRunResult result)
        {
            foreach (var controlType in CoreControlTypes)
            {
                RunCheck(result, $"Interacao:Modulo:{controlType.Name}", () =>
                {
                    ExerciseHostedElementButtons(controlType);
                });
            }
        }

        private void RunInteractiveDiscoveredUserControlChecks(UiSmokeTestRunResult result)
        {
            var excludedTypes = new HashSet<Type>(CoreControlTypes);
            var controlTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(UserControl).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null &&
                    !excludedTypes.Contains(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToList();

            foreach (var controlType in controlTypes)
            {
                RunCheck(result, $"Interacao:Controle:{controlType.Name}", () =>
                {
                    ExerciseHostedElementButtons(controlType);
                });
            }
        }

        private void RunInteractiveWindowButtonChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            var discoveredExcludedTypes = new HashSet<Type>
            {
                typeof(MainWindow),
                typeof(ImportarNotaWindow),
                typeof(AjusteEstoqueWindow),
                typeof(ConfigurarPermissoesWindow),
                typeof(GerenciarPerfisWindow),
                typeof(EditarFornecedorWindow),
                typeof(EditarFuncionarioWindow),
                typeof(EditarClienteWindow),
                typeof(EditarProdutoWindow),
                typeof(HistoricoClienteWindow),
                typeof(NovoFuncionarioWindow),
                typeof(NovoOrcamentoWindow),
                typeof(NovoPerfilWindow),
                typeof(NovoVeiculoWindow),
                typeof(OrdemServicoWindow),
                typeof(SelecionarOrcamentoWindow),
                typeof(VisualizarFornecedorWindow),
                typeof(VisualizarVeiculoWindow),
                typeof(AdicionarFornecedorDialog)
            };

            var discoveredWindows = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(Window).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null &&
                    !discoveredExcludedTypes.Contains(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(type => ($"Interacao:Janela:{type.Name}", WindowType: type, Factory: new Func<Window>(() => (Window)Activator.CreateInstance(type)!)))
                .ToList();

            var safeParameterizedFactories = BuildParameterizedWindowFactories(syntheticUser)
                .Where(factory => factory.Name is not "Janela:ConfigurarPermissoesWindow"
                    and not "Janela:GerenciarPerfisWindow"
                    and not "Janela:NovoPerfilWindow"
                    and not "Janela:NovoFuncionarioWindow"
                    and not "Janela:EditarFuncionarioWindow"
                    and not "Janela:NovoVeiculoWindow"
                    and not "Janela:OperacaoCaixaWindow"
                    and not "Janela:SelecionarOrcamentoWindow"
                    and not "Janela:SelecionarVendaWindow"
                    and not "Janela:AdicionarFornecedorDialog")
                .Select(factory =>
                {
                    var previewWindow = factory.Factory();
                    var windowType = previewWindow.GetType();
                    previewWindow.Close();
                    return ($"Interacao:{factory.Name}", WindowType: windowType, Factory: factory.Factory);
                })
                .ToList();

            foreach (var factory in discoveredWindows.Concat(safeParameterizedFactories))
            {
                RunCheck(result, factory.Item1, () =>
                {
                    ExerciseWindowButtons(factory.Factory, factory.WindowType);
                });
            }

            var statefulFactories = BuildParameterizedWindowFactories(syntheticUser)
                .Where(factory => factory.Name is "Janela:ConfigurarPermissoesWindow" or "Janela:GerenciarPerfisWindow")
                .Select(factory =>
                {
                    var previewWindow = factory.Factory();
                    var windowType = previewWindow.GetType();
                    previewWindow.Close();
                    return ($"Interacao:{factory.Name}", WindowType: windowType, Factory: factory.Factory);
                })
                .ToList();

            foreach (var factory in statefulFactories)
            {
                RunCheck(result, factory.Item1, () =>
                {
                    ExerciseWindowButtons(factory.Factory, factory.WindowType);
                });
            }
        }

    }
}
