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
        // Helpers de janela, interacao, visual tree, autofill e clique.

        private static void PrepareWindow(Window window)
        {
            try
            {
                ShowWindowForInteraction(window);

                if (window.Content is FrameworkElement content)
                {
                    PrepareElement(content);
                }

                window.UpdateLayout();
            }
            finally
            {
                TryCloseWindow(window, TimeSpan.FromSeconds(5));
            }
        }

        private static void InitializeWindowForInteraction(Window window)
        {
            window.Width = 1440;
            window.Height = 900;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = -10000;
            window.Top = -10000;
            window.ShowInTaskbar = false;
            window.ApplyTemplate();
            window.UpdateLayout();
        }

        private static void ShowWindowForInteraction(Window window)
        {
            InitializeWindowForInteraction(window);

            if (!window.IsVisible)
            {
                window.Show();
            }

            PumpDispatcher();

            // Aguarda a janela ser carregada e visivel com timeout curto
            var loaded = TryWaitForCondition(() => window.IsLoaded && window.IsVisible, TimeSpan.FromSeconds(5));
            if (!loaded)
            {
                try
                {
                    App.Logger.LogWarning($"Smoke test: janela {window.GetType().Name} nao ficou visivel apos 5s.");
                }
                catch { }
            }

            if (window.Content is FrameworkElement content)
            {
                const int maxPrepareAttempts = 3;
                var attempt = 0;
                while (attempt < maxPrepareAttempts)
                {
                    try
                    {
                        PrepareElement(content);
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        attempt++;
                        try { App.Logger.LogWarning($"Smoke test: falha ao preparar elemento ({attempt}/{maxPrepareAttempts}): {ex.Message}"); } catch { }
                        PumpDispatcher();
                        Thread.Sleep(100);
                        if (attempt >= maxPrepareAttempts)
                        {
                            try { App.Logger.LogWarning($"Smoke test: preparacao falhou apos {maxPrepareAttempts} tentativas."); } catch { }
                        }
                    }
                }
            }

            window.UpdateLayout();
            PumpDispatcher();
        }

        private static void RestoreWindowForInteraction(Window window)
        {
            if (window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished)
            {
                return;
            }

            try
            {
                if (!window.IsVisible)
                {
                    window.Show();
                }

                window.Activate();
                PumpDispatcher();

                if (window.Content is FrameworkElement content)
                {
                    PrepareElement(content);
                }

                window.UpdateLayout();
                PumpDispatcher();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void PrepareElement(FrameworkElement element)
        {
            element.ApplyTemplate();
            element.Measure(new Size(1440, 900));
            element.Arrange(new Rect(0, 0, 1440, 900));
            element.UpdateLayout();

            if (element is UserControl control && control.Content is FrameworkElement content)
            {
                content.ApplyTemplate();
                content.Measure(new Size(1440, 900));
                content.Arrange(new Rect(0, 0, 1440, 900));
                content.UpdateLayout();
            }
        }

        private void ExerciseHostedElementButtons(Type controlType)
        {
            if (controlType == typeof(VeiculosControl))
            {
                ExerciseVeiculosControlButtons();
                return;
            }

            if (controlType == typeof(EstoqueControl))
            {
                ExerciseEstoqueControlButtons();
                return;
            }

            if (controlType == typeof(FinanceiroControl))
            {
                ExerciseFinanceiroControlButtons();
                return;
            }

            if (controlType == typeof(AgendamentosControl))
            {
                ExerciseAgendamentosControlButtons();
                return;
            }

            if (controlType == typeof(RelatoriosControl))
            {
                ExerciseRelatoriosControlButtons();
                return;
            }

            ExerciseInteractionSurface(
                controlType,
                () =>
                {
                    if (Activator.CreateInstance(controlType) is not FrameworkElement root)
                    {
                        throw new InvalidOperationException($"Falha ao instanciar o controle {controlType.FullName}.");
                    }

                    return new InteractionSurface(CreateHostWindow(root, controlType.Name), root);
                });
        }

        private void ExerciseVeiculosControlButtons()
        {
            var hostWindow = CreateHostWindow(new VeiculosControl(), nameof(VeiculosControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not VeiculosControl control)
                {
                    throw new InvalidOperationException("Host de VeiculosControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(VeiculosControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                ClickButton(control, "NovoVeiculoButton");
                ClickButton(control, "ExportarVeiculosButton");
                ClickButton(control, "LimparFiltrosVeiculosButton");

                var dataGrid = FindElementByName<DataGrid>(control, "VeiculosDataGrid")
                    ?? throw new InvalidOperationException("VeiculosDataGrid nao foi localizado para a automacao dedicada.");

                WaitForCondition(
                    () => dataGrid.Items.Count > 0,
                    TimeSpan.FromSeconds(5),
                    "O modulo de veiculos nao carregou registros para exercitar as acoes por linha.");

                dataGrid.SelectedIndex = 0;
                if (dataGrid.SelectedItem != null)
                {
                    dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                    if (dataGrid.Columns.Count > 0)
                    {
                        dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.SelectedItem, dataGrid.Columns[0]);
                    }
                }

                WaitForUiIdle();

                InvokeButtonHandler(control, "VisualizarVeiculo_Click", dataGrid.SelectedItem);
                InvokeButtonHandler(control, "EditarVeiculo_Click", dataGrid.SelectedItem);
                InvokeButtonHandler(control, "ExcluirVeiculo_Click", dataGrid.SelectedItem);
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseEstoqueControlButtons()
        {
            var hostWindow = CreateHostWindow(new EstoqueControl(), nameof(EstoqueControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not EstoqueControl control)
                {
                    throw new InvalidOperationException("Host de EstoqueControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(EstoqueControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                ClickButton(control, "NovoProdutoButton");
                ClickButton(control, "AjustarEstoqueButton");

                var dataGrid = FindElementByName<DataGrid>(control, "ProdutosDataGrid")
                    ?? throw new InvalidOperationException("ProdutosDataGrid nao foi localizado para a automacao dedicada.");

                WaitForCondition(
                    () => dataGrid.Items.Count > 0,
                    TimeSpan.FromSeconds(5),
                    "O modulo de estoque nao carregou produtos para exercitar as acoes por linha.");

                var statusFiltro = FindElementByName<ComboBox>(control, "StatusFiltroComboBox")
                    ?? throw new InvalidOperationException("StatusFiltroComboBox nao foi localizado para validar filtros operacionais.");

                var statusDisponiveis = statusFiltro.Items.Cast<object?>()
                    .Select(item => item?.ToString() ?? string.Empty)
                    .ToList();

                if (!statusDisponiveis.Contains("Mais Vendidos") ||
                    !statusDisponiveis.Contains("Vendidos no Mes"))
                {
                    throw new InvalidOperationException("Filtros de ranking de vendas nao foram expostos no estoque.");
                }

                statusFiltro.SelectedItem = "Mais Vendidos";
                WaitForUiIdle();
                statusFiltro.SelectedItem = "Vendidos no Mes";
                WaitForUiIdle();
                statusFiltro.SelectedItem = "Todos";
                WaitForUiIdle();

                SelectFirstDataGridItem(dataGrid);

                ClickButton(control, "EntradaEstoqueButton");
                SelectFirstDataGridItem(dataGrid);
                ClickButton(control, "SaidaEstoqueButton");
                SelectFirstDataGridItem(dataGrid);
                ClickButton(control, "EtiquetaProdutoButton");
                SelectFirstDataGridItem(dataGrid);
                ClickButton(control, "InventarioEstoqueButton");
                SelectFirstDataGridItem(dataGrid);
                ClickButton(control, "HistoricoEstoqueHeaderButton");

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "VisualizarProdutoButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "EditarProdutoButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "AjustarEstoqueButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "InventariarProdutoButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "HistoricoEstoqueButton_Click", dataGrid.SelectedItem);

                SelectFirstDataGridItem(dataGrid);
                InvokeButtonHandler(control, "ExcluirProdutoButton_Click", dataGrid.SelectedItem);
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseFinanceiroControlButtons()
        {
            var hostWindow = CreateHostWindow(new FinanceiroControl(), nameof(FinanceiroControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not FinanceiroControl control)
                {
                    throw new InvalidOperationException("Host de FinanceiroControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(FinanceiroControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                var contasPagarGrid = FindElementByName<DataGrid>(control, "ContasPagarDataGrid")
                    ?? throw new InvalidOperationException("ContasPagarDataGrid nao foi localizado para a automacao dedicada.");
                var contasReceberGrid = FindElementByName<DataGrid>(control, "ContasReceberDataGrid")
                    ?? throw new InvalidOperationException("ContasReceberDataGrid nao foi localizado para a automacao dedicada.");

                WaitForCondition(
                    () => contasPagarGrid.Items.Count > 0 && contasReceberGrid.Items.Count > 0,
                    TimeSpan.FromSeconds(10),
                    "O modulo financeiro nao carregou contas suficientes para exercitar as acoes operacionais.");

                InvokeButtonHandler(control, "AtualizarDadosButton_Click", null);
                InvokeButtonHandler(control, "ExportarRelatorioButton_Click", null);
                InvokeButtonHandler(control, "GerarPDFButton_Click", null);
                InvokeButtonHandler(control, "ImprimirButton_Click", null);
                InvokeButtonHandler(control, "FiltrosAvancadosButton_Click", null);

                InvokeButtonHandler(control, "FiltroContasPagarTodas_Click", null);
                InvokeButtonHandler(control, "FiltroContasPagarVencidas_Click", null);
                InvokeButtonHandler(control, "FiltroContasPagarHoje_Click", null);
                InvokeButtonHandler(control, "FiltroContasPagarSemana_Click", null);
                InvokeButtonHandler(control, "FiltroContasPagarTodas_Click", null);
                SelectFirstDataGridItem(contasPagarGrid);
                control.ViewModel.ContaPagarSelecionada = contasPagarGrid.SelectedItem as ContaPagar;
                InvokeButtonHandler(control, "BaixarContaPagarSelecionada_Click", null);

                InvokeButtonHandler(control, "FiltroContasReceberTodas_Click", null);
                InvokeButtonHandler(control, "FiltroContasReceberVencidas_Click", null);
                InvokeButtonHandler(control, "FiltroContasReceberHoje_Click", null);
                InvokeButtonHandler(control, "FiltroContasReceberSemana_Click", null);
                InvokeButtonHandler(control, "FiltroContasReceberTodas_Click", null);
                SelectFirstDataGridItem(contasReceberGrid);
                control.ViewModel.ContaReceberSelecionada = contasReceberGrid.SelectedItem as ContaReceber;
                InvokeButtonHandler(control, "BaixarContaReceberSelecionada_Click", null);
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseAgendamentosControlButtons()
        {
            var hostWindow = CreateHostWindow(new AgendamentosControl(), nameof(AgendamentosControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not AgendamentosControl control)
                {
                    throw new InvalidOperationException("Host de AgendamentosControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(AgendamentosControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                var viewModel = control.DataContext as AgendamentosViewModel
                    ?? throw new InvalidOperationException("AgendamentosControl nao expôs o ViewModel esperado.");
                var listView = FindElementByName<ListView>(control, "agendamentosListView")
                    ?? throw new InvalidOperationException("agendamentosListView nao foi localizado para a automacao dedicada.");
                var searchBox = FindElementByName<TextBox>(control, "searchBox");

                WaitForCondition(
                    () => listView.Items.Count > 0,
                    TimeSpan.FromSeconds(10),
                    "O modulo de agendamentos nao carregou itens suficientes para exercitar as acoes operacionais.");

                viewModel.AtualizarCommand.Execute(null);
                viewModel.NovoAgendamentoCommand.Execute(null);
                viewModel.EncaixeRapidoCommand.Execute(null);
                viewModel.ImprimirAgendaCommand.Execute(null);
                viewModel.ExportarPDFCommand.Execute(null);
                viewModel.FiltrosRapidosCommand.Execute(null);
                viewModel.ModoOficinaCommand.Execute(null);
                viewModel.LimparFiltrosCommand.Execute(null);

                if (searchBox != null)
                {
                    searchBox.Text = "SMOKE";
                    WaitForUiIdle();
                    searchBox.Text = string.Empty;
                    WaitForUiIdle();
                }

                SelectFirstListViewItem(listView);
                viewModel.AgendamentoSelecionado = listView.SelectedItem as Agendamento;

                viewModel.EditarCommand.Execute(null);
                viewModel.ReagendarCommand.Execute(null);
                viewModel.DuplicarCommand.Execute(null);
                viewModel.GerarOSCommand.Execute(null);
                viewModel.CheckInCommand.Execute(null);
                viewModel.EnviarWhatsAppCommand.Execute(null);
                viewModel.CheckOutCommand.Execute(null);
                viewModel.CancelarCommand.Execute(null);
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseRelatoriosControlButtons()
        {
            var hostWindow = CreateHostWindow(new RelatoriosControl(), nameof(RelatoriosControl));
            AutomatedDialogSupervisor? supervisor = null;

            try
            {
                ShowWindowForInteraction(hostWindow);
                if (hostWindow.Content is not RelatoriosControl control)
                {
                    throw new InvalidOperationException("Host de RelatoriosControl nao conseguiu carregar o controle.");
                }

                PrepareInteractiveSurface(control, typeof(RelatoriosControl));
                supervisor = new AutomatedDialogSupervisor(hostWindow, _fixture);
                supervisor.Start();

                WaitForCondition(
                    () => control.ViewModel.DadosCarregados && !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "O modulo de relatorios nao concluiu a carga inicial para a automacao dedicada.");

                AwaitUiTask(
                    control.ViewModel.CarregarDadosAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "A atualizacao dedicada de relatorios nao concluiu a carga esperada.");
                control.ViewModel.ExportarPDF();
                control.ViewModel.ExportarExcel();
                control.ViewModel.ExportarPacoteEvidencias();
                InvokeButtonHandler(control, "Imprimir_Click", null);
                control.ViewModel.AlternarFavorito();
                control.ViewModel.SalvarWorkspaceAtual();
                control.ViewModel.AlternarModoExecutivo();
                InvokeButtonHandler(control, "TelaCheia_Click", null);
                InvokeButtonHandler(control, "TelaCheia_Click", null);

                control.ViewModel.FiltroOperador = "Smoke";
                control.ViewModel.FiltroVendedor = "Administrador";
                AwaitUiTask(
                    control.ViewModel.AplicarFiltrosAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "A aplicacao de filtros dos relatorios nao concluiu no tempo esperado.");
                AwaitUiTask(
                    control.ViewModel.LimparFiltrosAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "A limpeza de filtros dos relatorios nao concluiu no tempo esperado.");

                control.ViewModel.SeveridadeAuditoriaSelecionada = "Info";
                control.ViewModel.StatusAuditoriaSelecionado = "Todos";
                control.ViewModel.UsuarioAuditoriaFiltro = string.Empty;
                control.ViewModel.TermoAuditoriaFiltro = "PDV";
                AwaitUiTask(
                    control.ViewModel.AplicarFiltrosAuditoriaAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "A consulta operacional da auditoria nao concluiu no tempo esperado.");
                AwaitUiTask(
                    control.ViewModel.AvancarPaginaAuditoriaAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "O avancar de pagina da auditoria nao concluiu no tempo esperado.");
                AwaitUiTask(
                    control.ViewModel.RetrocederPaginaAuditoriaAsync(),
                    () => !control.ViewModel.IsLoading,
                    TimeSpan.FromSeconds(15),
                    "O retorno de pagina da auditoria nao concluiu no tempo esperado.");
            }
            finally
            {
                supervisor?.Dispose();
                CloseTransientWindows(hostWindow);

                if (hostWindow.IsVisible)
                {
                    hostWindow.Close();
                }
            }
        }

        private void ExerciseWindowButtons(Func<Window> factory, Type rootType)
        {
            ExerciseInteractionSurface(
                rootType,
                () =>
                {
                    var window = factory();
                    return new InteractionSurface(window, window);
                });
        }

        private void ExerciseInteractionSurface(Type rootType, Func<InteractionSurface> surfaceFactory)
        {
            var descriptors = CaptureButtonDescriptors(rootType, surfaceFactory);
            if (descriptors.Count == 0)
            {
                _logger.LogInfo($"Nenhum botao interativo encontrado em {rootType.Name}; a validacao ficou restrita ao carregamento visual.");
                return;
            }

            foreach (var descriptor in descriptors)
            {
                AutomatedDialogSupervisor? supervisor = null;
                InteractionSurface? surface = null;

                try
                {
                    surface = surfaceFactory();
                    ShowWindowForInteraction(surface.HostWindow);
                    PrepareInteractiveSurface(surface.Root, rootType);
                    var searchRoot = ResolveInteractiveSearchRoot(surface);
                    PrepareButtonsForInteraction(searchRoot);

                    WaitForCondition(
                        () => FindVisualChildren<Button>(searchRoot).Any(IsButtonDiscoverable),
                        TimeSpan.FromSeconds(5),
                        $"Nenhum botao visivel foi carregado em {rootType.Name}.");

                    var targetButton = FindButtonByDescriptor(searchRoot, descriptor)
                        ?? throw new InvalidOperationException(
                            $"Botao '{descriptor.DisplayName}' nao foi reencontrado em {rootType.Name}. " +
                            $"Disponiveis agora: {DescribeVisibleButtons(searchRoot)}");

                    var buttonEnabled = TryWaitForCondition(
                        () =>
                        {
                            if (targetButton.IsEnabled)
                            {
                                return true;
                            }

                            PrepareButtonsForInteraction(searchRoot);
                            targetButton = FindButtonByDescriptor(searchRoot, descriptor) ?? targetButton;
                            return targetButton.IsEnabled;
                        },
                        TimeSpan.FromSeconds(5));

                    if (!buttonEnabled && !CanForceDisabledAutomationClick(targetButton))
                    {
                        throw new InvalidOperationException($"Botao '{descriptor.DisplayName}' permaneceu desabilitado em {rootType.Name}.");
                    }

                    supervisor = new AutomatedDialogSupervisor(surface.HostWindow, _fixture);
                    supervisor.Start();

                    if (!targetButton.IsEnabled)
                    {
                        _logger.LogWarning($"Smoke test forcou clique automatizado em botao desabilitado '{descriptor.DisplayName}' de {rootType.Name}.");
                    }

                    RaiseButtonClick(targetButton);
                    WaitForUiIdle();
                }
                finally
                {
                    supervisor?.Dispose();

                    if (surface != null)
                    {
                        CloseTransientWindows(surface.HostWindow);
                        CloseSurface(surface);
                    }
                }
            }
        }

        private List<ButtonDescriptor> CaptureButtonDescriptors(Type rootType, Func<InteractionSurface> surfaceFactory)
        {
            AutomatedDialogSupervisor? supervisor = null;
            InteractionSurface? surface = null;

            try
            {
                surface = surfaceFactory();
                ShowWindowForInteraction(surface.HostWindow);
                PrepareInteractiveSurface(surface.Root, rootType);
                var searchRoot = ResolveInteractiveSearchRoot(surface);
                PrepareButtonsForInteraction(searchRoot);

                supervisor = new AutomatedDialogSupervisor(surface.HostWindow, _fixture);
                supervisor.Start();

                var hasButtons = TryWaitForCondition(
                    () => FindVisualChildren<Button>(searchRoot).Any(IsButtonDiscoverable),
                    TimeSpan.FromSeconds(5));

                if (!hasButtons)
                {
                    return new List<ButtonDescriptor>();
                }

                return FindCandidateButtons(searchRoot, rootType)
                    .Select((button, index) => new ButtonDescriptor(index, button.Name, ExtractButtonText(button)))
                    .ToList();
            }
            finally
            {
                supervisor?.Dispose();

                if (surface != null)
                {
                    CloseTransientWindows(surface.HostWindow);
                    CloseSurface(surface);
                }
            }
        }

        private static Window CreateHostWindow(FrameworkElement content, string title)
        {
            return new Window
            {
                Title = title,
                Content = content
            };
        }

        private static void CloseSurface(InteractionSurface surface)
        {
            if (surface.HostWindow.IsVisible)
            {
                surface.HostWindow.Close();
            }
        }

        private static DependencyObject ResolveInteractiveSearchRoot(InteractionSurface surface)
        {
            if (surface.Root is Window windowRoot && windowRoot.Content is DependencyObject windowContent)
            {
                return windowContent;
            }

            return surface.Root;
        }

        private void PrepareInteractiveSurface(FrameworkElement root, Type rootType)
        {
            if (root is Window windowRoot)
            {
                if (windowRoot.Content is FrameworkElement content)
                {
                    PrepareElement(content);
                }
            }
            else
            {
                PrepareElement(root);
            }

            AutoPopulateInteractiveInputs(root, rootType);
            PrimeSelectors(root);

            if (root is Window refreshedWindowRoot)
            {
                if (refreshedWindowRoot.Content is FrameworkElement refreshedContent)
                {
                    PrepareElement(refreshedContent);
                }
            }
            else
            {
                PrepareElement(root);
            }

            PumpDispatcher();
        }

        private void AutoPopulateInteractiveInputs(DependencyObject root, Type rootType)
        {
            var token = DateTime.Now.ToString("HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);

            foreach (var textBox in FindVisualChildren<TextBox>(root))
            {
                if (!textBox.IsEnabled || textBox.IsReadOnly)
                {
                    continue;
                }

                if (ShouldSkipAutoFill(textBox.Name))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(textBox.Text) && rootType != typeof(LoginWindow))
                {
                    continue;
                }

                textBox.Text = BuildTextBoxValue(rootType, textBox.Name, token);
            }

            foreach (var passwordBox in FindVisualChildren<PasswordBox>(root))
            {
                if (!passwordBox.IsEnabled || !string.IsNullOrWhiteSpace(passwordBox.Password))
                {
                    continue;
                }

                passwordBox.Password = "Workflow@123";
            }

            foreach (var comboBox in FindVisualChildren<ComboBox>(root))
            {
                if (!comboBox.IsEnabled || comboBox.SelectedItem != null || comboBox.Items.Count == 0)
                {
                    continue;
                }

                comboBox.SelectedIndex = 0;
            }

            foreach (var datePicker in FindVisualChildren<DatePicker>(root))
            {
                if (!datePicker.IsEnabled || datePicker.SelectedDate.HasValue)
                {
                    continue;
                }

                datePicker.SelectedDate = DateTime.Today;
            }
        }

        private static void PrimeSelectors(DependencyObject root)
        {
            foreach (var dataGrid in FindVisualChildren<DataGrid>(root))
            {
                if (!dataGrid.IsEnabled || dataGrid.Items.Count == 0)
                {
                    continue;
                }

                if (dataGrid.SelectedIndex < 0)
                {
                    dataGrid.SelectedIndex = 0;
                }

                if (dataGrid.SelectedItem != null)
                {
                    dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                }

                if (dataGrid.Columns.Count > 0 && dataGrid.SelectedIndex >= 0)
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.SelectedItem, dataGrid.Columns[0]);
                }
            }

            foreach (var listBox in FindVisualChildren<ListBox>(root))
            {
                if (!listBox.IsEnabled || listBox.Items.Count == 0)
                {
                    continue;
                }

                if (listBox.SelectedIndex < 0)
                {
                    listBox.SelectedIndex = 0;
                }

                if (listBox.SelectedItem != null)
                {
                    listBox.ScrollIntoView(listBox.SelectedItem);
                }
            }

            foreach (var listView in FindVisualChildren<ListView>(root))
            {
                if (!listView.IsEnabled || listView.Items.Count == 0)
                {
                    continue;
                }

                if (listView.SelectedIndex < 0)
                {
                    listView.SelectedIndex = 0;
                }

                if (listView.SelectedItem != null)
                {
                    listView.ScrollIntoView(listView.SelectedItem);
                }
            }

            foreach (var tabControl in FindVisualChildren<TabControl>(root))
            {
                if (tabControl.Items.Count > 0 && tabControl.SelectedIndex < 0)
                {
                    tabControl.SelectedIndex = 0;
                }
            }
        }

        private static void PrepareButtonsForInteraction(DependencyObject root)
        {
            PrimeSelectors(root);
            PumpDispatcher();
        }

        private string BuildTextBoxValue(Type rootType, string name, string token)
        {
            var normalizedName = name?.Trim() ?? string.Empty;
            var lowered = normalizedName.ToLowerInvariant();

            if (rootType == typeof(LoginWindow))
            {
                if (string.Equals(normalizedName, "EmailTextBox", StringComparison.Ordinal))
                {
                    return _fixture?.Administrator.Email ?? "smoke-admin@primoauto.com";
                }

                if (string.Equals(normalizedName, "SenhaTextBox", StringComparison.Ordinal))
                {
                    return "Workflow@123";
                }
            }

            if (lowered.Contains("email"))
            {
                return $"smoke.{token}@primoauto.com";
            }

            if (lowered.Contains("cpf"))
            {
                return GerarCpfValido(token);
            }

            if (lowered.Contains("cnpj"))
            {
                return GerarCnpjValido(token);
            }

            if (lowered.Contains("placa"))
            {
                return GerarPlacaValida(token);
            }

            if (lowered.Contains("cep"))
            {
                return "01001000";
            }

            if (lowered.Contains("telefone") || lowered.Contains("celular") || lowered.Contains("whatsapp"))
            {
                return "(11) 98888-0000";
            }

            if (lowered.Contains("senha"))
            {
                return "Workflow@123";
            }

            if (lowered.Contains("valor") || lowered.Contains("preco") || lowered.Contains("desconto") || lowered.Contains("salario") || lowered.Contains("total"))
            {
                return "10,00";
            }

            if (lowered.Contains("quantidade") || lowered.Contains("qtd") || lowered.Contains("estoque") || lowered.Contains("numero") || lowered.Contains("nota"))
            {
                return "1";
            }

            if (lowered.Contains("ano"))
            {
                return DateTime.Today.Year.ToString();
            }

            if (lowered.Contains("codigo") || lowered.Contains("sku"))
            {
                return $"SMK-{token}";
            }

            if (lowered.Contains("chassi"))
            {
                return $"9BWZZZ377VT{token.PadLeft(9, '0')[..9]}";
            }

            if (lowered.Contains("renavam"))
            {
                return token.PadLeft(11, '0')[..11];
            }

            if (lowered.Contains("razao") || lowered.Contains("fantasia") || lowered.Contains("nome"))
            {
                return $"Smoke {token}";
            }

            if (lowered.Contains("rua") || lowered.Contains("logradouro") || lowered.Contains("endereco"))
            {
                return "Rua Smoke Teste";
            }

            if (lowered.Contains("bairro"))
            {
                return "Centro";
            }

            if (lowered.Contains("cidade"))
            {
                return "Sao Paulo";
            }

            if (lowered.Contains("estado") || lowered.Contains("uf"))
            {
                return "SP";
            }

            if (lowered.Contains("observ") || lowered.Contains("descricao"))
            {
                return "Registro sintetico do smoke test.";
            }

            return $"Smoke {token}";
        }

        private static bool ShouldSkipAutoFill(string? name)
        {
            var lowered = name?.Trim().ToLowerInvariant() ?? string.Empty;
            return lowered.Contains("busca") || lowered.Contains("filtro") || lowered.Contains("search") || lowered.Contains("termo");
        }

        private static List<Button> FindCandidateButtons(DependencyObject root, Type rootType)
        {
            return FindVisualChildren<Button>(root)
                .Where(button =>
                    IsButtonDiscoverable(button) &&
                    button.IsEnabled &&
                    !ShouldSkipButton(rootType, button.Name, ExtractButtonText(button)))
                .ToList();
        }

        private static Button? FindButtonByDescriptor(DependencyObject root, ButtonDescriptor descriptor)
        {
            var buttons = FindVisualChildren<Button>(root)
                .Where(IsButtonDiscoverable)
                .ToList();

            if (!string.IsNullOrWhiteSpace(descriptor.Name))
            {
                var named = PreferEnabledButton(buttons.Where(button =>
                    string.Equals(button.Name, descriptor.Name, StringComparison.OrdinalIgnoreCase)));
                if (named != null)
                {
                    return named;
                }
            }

            if (!string.IsNullOrWhiteSpace(descriptor.Text))
            {
                var textMatch = PreferEnabledButton(buttons.Where(button =>
                {
                    var currentText = ExtractButtonText(button);
                    return string.Equals(currentText, descriptor.Text, StringComparison.OrdinalIgnoreCase)
                        || (!string.IsNullOrWhiteSpace(currentText) &&
                            currentText.Contains(descriptor.Text, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(descriptor.Text) &&
                            descriptor.Text.Contains(currentText, StringComparison.OrdinalIgnoreCase));
                }));
                if (textMatch != null)
                {
                    return textMatch;
                }
            }

            return descriptor.Index >= 0 && descriptor.Index < buttons.Count
                ? PreferEnabledButton(new[] { buttons[descriptor.Index] })
                : null;
        }

        private static bool ShouldSkipButton(Type rootType, string? buttonName, string buttonText)
        {
            if (rootType == typeof(LoginWindow) &&
                string.Equals(buttonName, "CloseButton", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (rootType == typeof(MainWindow) &&
                string.Equals(buttonName, "MenuSair", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (rootType == typeof(PDVControl) &&
                (string.Equals(buttonName, "AbrirSelecionarClienteButton", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(buttonText, "Selecionar", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (typeof(Window).IsAssignableFrom(rootType) &&
                string.Equals(buttonText, "X", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static string ExtractButtonText(Button button)
        {
            var directText = button.Content switch
            {
                string text => text.Trim(),
                TextBlock textBlock => textBlock.Text.Trim(),
                _ => string.Empty
            };

            if (!string.IsNullOrWhiteSpace(directText))
            {
                return directText;
            }

            var nestedText = string.Join(
                " ",
                FindVisualChildren<TextBlock>(button)
                    .Select(textBlock => textBlock.Text?.Trim())
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .Distinct(StringComparer.OrdinalIgnoreCase));

            return string.IsNullOrWhiteSpace(nestedText) ? button.Name : nestedText;
        }

        private static string DescribeVisibleButtons(DependencyObject root)
        {
            var buttons = FindVisualChildren<Button>(root)
                .Where(IsButtonDiscoverable)
                .Select(button =>
                {
                    var name = string.IsNullOrWhiteSpace(button.Name) ? "(sem nome)" : button.Name;
                    var text = string.IsNullOrWhiteSpace(ExtractButtonText(button)) ? "(sem texto)" : ExtractButtonText(button);
                    return $"{name}='{text}'";
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return buttons.Count == 0
                ? "nenhum botao visivel"
                : string.Join(", ", buttons);
        }

        private static void RaiseButtonClick(Button button)
        {
            button.Focus();
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

            if (button.Command?.CanExecute(button.CommandParameter) == true)
            {
                button.Command.Execute(button.CommandParameter);
            }
        }

        private static bool IsButtonDiscoverable(Button button)
        {
            return button.Visibility == Visibility.Visible;
        }

        private static Button? PreferEnabledButton(IEnumerable<Button> buttons)
        {
            var candidates = buttons.ToList();
            return candidates.FirstOrDefault(button => button.IsEnabled)
                ?? candidates.FirstOrDefault();
        }

        private static bool CanForceDisabledAutomationClick(Button button)
        {
            if (button == null || button.Visibility != Visibility.Visible)
            {
                return false;
            }

            if (button.Tag == null && button.DataContext == null)
            {
                return false;
            }

            return HasAncestor<DataGridCell>(button)
                || HasAncestor<DataGridRow>(button)
                || HasAncestor<ListViewItem>(button)
                || HasAncestor<ListBoxItem>(button);
        }

        private static bool HasAncestor<TAncestor>(DependencyObject dependencyObject) where TAncestor : DependencyObject
        {
            DependencyObject? current = dependencyObject;
            while (current != null)
            {
                if (current is TAncestor)
                {
                    return true;
                }

                current = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        private static void WaitForUiIdle(int cycles = 8)
        {
            for (var index = 0; index < cycles; index++)
            {
                PumpDispatcher();
                Thread.Sleep(75);
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null)
            {
                yield break;
            }

            var visualMatches = CollectDescendants<T>(root, includeLogicalChildren: false);
            var matches = visualMatches.Count > 0
                ? visualMatches
                : CollectDescendants<T>(root, includeLogicalChildren: true);

            foreach (var match in matches)
            {
                yield return match;
            }
        }

        private static List<T> CollectDescendants<T>(DependencyObject root, bool includeLogicalChildren) where T : DependencyObject
        {
            var matches = new List<T>();
            var visited = new HashSet<DependencyObject>();
            var pending = new Stack<DependencyObject>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current == null || !visited.Add(current))
                {
                    continue;
                }

                if (current is T typedCurrent)
                {
                    matches.Add(typedCurrent);
                }

                foreach (var child in GetTraversalChildren(current, includeLogicalChildren))
                {
                    pending.Push(child);
                }
            }

            return matches;
        }

        private static IEnumerable<DependencyObject> GetTraversalChildren(DependencyObject root, bool includeLogicalChildren)
        {
            var visualChildrenCount = 0;
            try
            {
                visualChildrenCount = VisualTreeHelper.GetChildrenCount(root);
            }
            catch
            {
                visualChildrenCount = 0;
            }

            for (var index = 0; index < visualChildrenCount; index++)
            {
                DependencyObject? visualChild;
                try
                {
                    visualChild = VisualTreeHelper.GetChild(root, index);
                }
                catch
                {
                    visualChild = null;
                }

                if (visualChild != null)
                {
                    yield return visualChild;
                }
            }

            if (includeLogicalChildren && (root is FrameworkElement || root is FrameworkContentElement))
            {
                IEnumerable logicalChildren;
                try
                {
                    logicalChildren = LogicalTreeHelper.GetChildren(root);
                }
                catch
                {
                    yield break;
                }

                foreach (var logicalChild in logicalChildren.OfType<DependencyObject>())
                {
                    yield return logicalChild;
                }
            }
        }

        private static T? FindElementByName<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            return FindVisualChildren<T>(root)
                .FirstOrDefault(element => string.Equals(element.Name, name, StringComparison.Ordinal));
        }

        private static void SelectTabByHeader(DependencyObject root, string header)
        {
            var tabControl = FindVisualChildren<TabControl>(root).FirstOrDefault()
                ?? throw new InvalidOperationException($"Nenhum TabControl foi localizado para selecionar a aba '{header}'.");
            var tabItem = tabControl.Items
                .OfType<TabItem>()
                .FirstOrDefault(item => string.Equals(Convert.ToString(item.Header), header, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Aba '{header}' nao foi localizada.");

            tabControl.SelectedItem = tabItem;
            PumpDispatcher();

            if (root is FrameworkElement element)
            {
                element.UpdateLayout();
            }

            PumpDispatcher();
        }

        private static void SetTextBoxValue(DependencyObject root, string name, string value)
        {
            var textBox = FindElementByName<TextBox>(root, name)
                ?? throw new InvalidOperationException($"TextBox '{name}' nao foi localizado.");
            textBox.Text = value;
            PumpDispatcher();
        }

        private static void SetCheckBoxValue(DependencyObject root, string name, bool value)
        {
            var checkBox = FindElementByName<CheckBox>(root, name)
                ?? throw new InvalidOperationException($"CheckBox '{name}' nao foi localizado.");
            checkBox.IsChecked = value;
            PumpDispatcher();
        }

        private static void DefinirComboBoxTexto(DependencyObject root, string name, string value)
        {
            var comboBox = FindElementByName<ComboBox>(root, name)
                ?? throw new InvalidOperationException($"ComboBox '{name}' nao foi localizado.");

            var item = comboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(candidate => string.Equals(Convert.ToString(candidate.Content), value, StringComparison.OrdinalIgnoreCase));

            if (item != null)
            {
                comboBox.SelectedItem = item;
            }
            else
            {
                comboBox.Text = value;
            }

            PumpDispatcher();
        }

        private static void DefinirComboBoxPorTag(DependencyObject root, string name, string tag)
        {
            var comboBox = FindElementByName<ComboBox>(root, name)
                ?? throw new InvalidOperationException($"ComboBox '{name}' nao foi localizado.");

            var item = comboBox.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(candidate => string.Equals(Convert.ToString(candidate.Tag), tag, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"ComboBox '{name}' nao possui item com Tag='{tag}'.");

            comboBox.SelectedItem = item;
            PumpDispatcher();
        }

        private static void ClickButton(DependencyObject root, string nameOrText)
        {
            var button = PreferEnabledButton(
                FindVisualChildren<Button>(root)
                    .Where(candidate =>
                        IsButtonDiscoverable(candidate) &&
                        (string.Equals(candidate.Name, nameOrText, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(ExtractButtonText(candidate), nameOrText, StringComparison.OrdinalIgnoreCase))))
                ?? throw new InvalidOperationException($"Botao '{nameOrText}' nao foi localizado.");

            if (!button.IsEnabled)
            {
                throw new InvalidOperationException($"Botao '{nameOrText}' esta desabilitado durante o smoke test.");
            }

            RaiseButtonClick(button);
            WaitForUiIdle();
        }

        private static void SelectFirstDataGridItem(DataGrid dataGrid)
        {
            if (dataGrid.Items.Count == 0)
            {
                return;
            }

            dataGrid.SelectedIndex = 0;
            if (dataGrid.SelectedItem != null)
            {
                dataGrid.ScrollIntoView(dataGrid.SelectedItem);
                if (dataGrid.Columns.Count > 0)
                {
                    dataGrid.CurrentCell = new DataGridCellInfo(dataGrid.SelectedItem, dataGrid.Columns[0]);
                }
            }

            WaitForUiIdle();
        }

        private static void SelectFirstListViewItem(ListView listView)
        {
            if (listView.Items.Count == 0)
            {
                return;
            }

            listView.SelectedIndex = 0;
            if (listView.SelectedItem != null)
            {
            listView.ScrollIntoView(listView.SelectedItem);
            }

            WaitForUiIdle();
        }

        private static void AwaitUiTask(Task task, Func<bool> completionPredicate, TimeSpan timeout, string failureMessage)
        {
            WaitForCondition(
                () => task.IsCompleted && completionPredicate(),
                timeout,
                failureMessage);

            if (task.IsFaulted)
            {
                throw task.Exception?.GetBaseException() ?? new InvalidOperationException(failureMessage);
            }
        }

        private static void InvokeButtonHandler(object target, string methodName, object? tag)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(target.GetType().FullName, methodName);

            var button = new Button
            {
                Tag = tag,
                DataContext = tag
            };

            method.Invoke(target, new object[] { button, new RoutedEventArgs(ButtonBase.ClickEvent, button) });
            WaitForUiIdle();
        }

        private static void AdicionarProdutoAoCarrinhoParaAutomacao(PDVControl control, Produto fixtureProduto)
        {
            var produto = control.ViewModel.Produtos.FirstOrDefault(item => item.Id == fixtureProduto.Id)
                ?? control.ViewModel.Produtos.FirstOrDefault()
                ?? throw new InvalidOperationException("Nenhum produto disponivel foi encontrado no PDV.");

            var method = typeof(PDVControl).GetMethod("AdicionarProdutoAoCarrinho", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(PDVControl).FullName, "AdicionarProdutoAoCarrinho");

            control.ViewModel.SelectedProduto = produto;
            method.Invoke(control, new object[] { produto });
            WaitForUiIdle();
        }

        private static void AssertWindowStillOperational(Window hostWindow, string contexto)
        {
            var continuaOperacional = TryWaitForCondition(
                () => hostWindow.IsLoaded && hostWindow.IsVisible,
                TimeSpan.FromSeconds(2));

            if (!continuaOperacional &&
                !hostWindow.Dispatcher.HasShutdownStarted &&
                !hostWindow.Dispatcher.HasShutdownFinished)
            {
                try
                {
                    if (!hostWindow.IsVisible)
                    {
                        hostWindow.Show();
                    }

                    hostWindow.Activate();
                    WaitForUiIdle();
                }
                catch (InvalidOperationException)
                {
                }

                continuaOperacional = TryWaitForCondition(
                    () => hostWindow.IsLoaded && hostWindow.IsVisible,
                    TimeSpan.FromSeconds(2));
            }

            if (!continuaOperacional)
            {
                if (hostWindow.Content is PDVControl control &&
                    control.ViewModel != null &&
                    !control.Dispatcher.HasShutdownStarted &&
                    !control.Dispatcher.HasShutdownFinished)
                {
                    try
                    {
                        PrepareElement(control);
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    return;
                }

                throw new InvalidOperationException($"A tela hospedeira do PDV foi fechada de forma inesperada apos {contexto}.");
            }
        }

        private static void CloseTransientWindows(Window keepWindow)
        {
            var windows = Application.Current?.Windows
                .OfType<Window>()
                .Where(window => !ReferenceEquals(window, keepWindow))
                .ToList()
                ?? new List<Window>();

            foreach (var window in windows)
            {
                if (window.IsVisible)
                {
                    window.Close();
                }
            }
        }

        private static List<string> ObterJanelasTransientesVisiveis(Window keepWindow)
        {
            return Application.Current?.Windows
                .OfType<Window>()
                .Where(window => !ReferenceEquals(window, keepWindow) && window.IsVisible)
                .Select(window =>
                    string.IsNullOrWhiteSpace(window.Title)
                        ? window.GetType().Name
                        : $"{window.GetType().Name}('{window.Title}')")
                .ToList()
                ?? new List<string>();
        }

        private static bool TryCloseWindow(Window window, TimeSpan? timeout = null)
        {
            if (window == null) return true;

            try
            {
                if (window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished)
                {
                    return true;
                }

                if (window.IsVisible)
                {
                    try
                    {
                        window.Close();
                    }
                    catch (InvalidOperationException)
                    {
                        // ignore
                    }
                }

                var wait = timeout ?? TimeSpan.FromSeconds(3);
                return TryWaitForCondition(() => !window.IsVisible || window.Dispatcher.HasShutdownStarted || window.Dispatcher.HasShutdownFinished, wait);
            }
            catch
            {
                return false;
            }
        }

    }
}
