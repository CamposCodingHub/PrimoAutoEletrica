using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views.Clientes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.UserControls
{
    public partial class ClientesControl : UserControl
    {
        private readonly ClientesViewModel _viewModel;
        private readonly PermissionService _permissionService;

        public ClientesControl()
        {
            InitializeComponent();
            _viewModel = App.Services.GetRequiredService<ClientesViewModel>();
            DataContext = _viewModel;
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger);
            Focusable = true;
            PreviewKeyDown += ClientesControl_PreviewKeyDown;
            Loaded += (_, _) =>
            {
                AplicarFiltros();
                Dispatcher.BeginInvoke(new Action(() => BuscaClienteTextBox.Focus()), System.Windows.Threading.DispatcherPriority.Input);
            };
            CarregarClientes();
        }

        private void ClientesControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                NovoClienteButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                BuscaClienteTextBox.Focus();
                BuscaClienteTextBox.SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter
                && Keyboard.Modifiers == ModifierKeys.None
                && ClientesDataGrid.IsKeyboardFocusWithin
                && ObterClienteSelecionado() != null)
            {
                AbrirClienteSelecionado();
                e.Handled = true;
            }
        }

        private void AbrirClienteSelecionado()
        {
            var cliente = ObterClienteSelecionadoCompleto();
            if (cliente == null)
                return;

            var window = new VisualizarClienteWindow(cliente);
            WindowOwnerHelper.ConfigureOwner(window, this);
            window.ShowDialog();
        }

        private void CarregarClientes()
        {
            DefinirEstadoPainel(ClientesPainelEstado.Loading);

            try
            {
                _viewModel.LoadClients();
                AplicarFiltros();
                DefinirEstadoPainel(_viewModel.AllClientes.Count == 0
                    ? ClientesPainelEstado.Empty
                    : ClientesPainelEstado.Loaded);
            }
            catch (Exception ex)
            {
                ClientesErrorDescriptionText.Text = ex.Message;
                DefinirEstadoPainel(ClientesPainelEstado.Error);
                MessageBox.Show(
                    $"Erro ao carregar clientes:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private enum ClientesPainelEstado
        {
            Loading,
            Loaded,
            Empty,
            Error
        }

        private void DefinirEstadoPainel(ClientesPainelEstado estado)
        {
            ClientesLoadingPanel.Visibility = estado == ClientesPainelEstado.Loading ? Visibility.Visible : Visibility.Collapsed;
            ClientesErrorPanel.Visibility = estado == ClientesPainelEstado.Error ? Visibility.Visible : Visibility.Collapsed;
            ClientesEmptyPanel.Visibility = estado == ClientesPainelEstado.Empty ? Visibility.Visible : Visibility.Collapsed;
            ClientesContentGrid.Visibility = estado == ClientesPainelEstado.Loaded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RetryClientesButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarClientes();
        }

        private void AplicarFiltros()
        {
            var busca = BuscaClienteTextBox.Text?.Trim() ?? string.Empty;
            var filtro = (FiltroStatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";

            _viewModel.ApplyFilters(busca, filtro);
            AtualizarEstadoAcoesRapidas();
            AtualizarPainelLateral();
        }

        private void AtualizarIndicadores()
        {
            // Indicators are bound directly to the view model.
        }

        private void AtualizarPainelLateral()
        {
            var item = ObterClienteSelecionado();
            if (item == null)
            {
                InsightTicketTextBlock.Text = "Selecione um cliente para ver identidade, frota e histórico real.";
                InsightRetencaoTextBlock.Text = "Veículos vinculados aparecerão aqui.";
                InsightCrescimentoTextBlock.Text = "OS e indicadores usam consultas reais do domínio.";
                return;
            }

            var cliente = App.Repositories.Clientes.ObterPorId(item.Id) ?? item;
            var contato = string.IsNullOrWhiteSpace(cliente.Telefone) ? cliente.WhatsApp : cliente.Telefone;
            if (string.IsNullOrWhiteSpace(contato))
                contato = string.IsNullOrWhiteSpace(cliente.Email) ? "Sem contato" : cliente.Email;

            InsightTicketTextBlock.Text =
                $"{cliente.Nome}\n" +
                $"Contato: {contato}\n" +
                $"Documento: {(string.IsNullOrWhiteSpace(cliente.Documento) ? "Não informado" : cliente.Documento)}\n" +
                $"Status: {(cliente.Ativo ? (cliente.ClienteVip ? "VIP" : "Ativo") : "Inativo")}";

            if (cliente.Veiculos == null || cliente.Veiculos.Count == 0)
            {
                InsightRetencaoTextBlock.Text = "Nenhum veículo vinculado a este cliente.";
            }
            else
            {
                var frota = string.Join("\n", cliente.Veiculos
                    .Take(5)
                    .Select(v => $"• {v.Marca} {v.Modelo} · {v.Placa}".Trim()));
                InsightRetencaoTextBlock.Text =
                    $"Frota ({cliente.Veiculos.Count}):\n{frota}" +
                    (cliente.Veiculos.Count > 5 ? "\n…" : string.Empty);
            }

            try
            {
                var ordens = App.Repositories.OrdensServico.ObterPorClienteId(cliente.Id);
                var ultima = ordens.OrderByDescending(o => o.DataAbertura).FirstOrDefault();
                var totalHistorico = ordens.Sum(o => Math.Max(0, o.Itens.Sum(i => i.Total) - o.Desconto));
                InsightCrescimentoTextBlock.Text =
                    $"OS: {ordens.Count}\n" +
                    $"Última OS: {(ultima == null ? "—" : $"{ultima.Numero} · {ultima.Status} · {ultima.DataAbertura:dd/MM/yyyy}")}\n" +
                    $"Total histórico OS: {totalHistorico:C}\n" +
                    $"Última visita: {(cliente.UltimaVisita.HasValue ? cliente.UltimaVisita.Value.ToString("dd/MM/yyyy") : "—")}\n" +
                    $"Total gasto (cadastro): {cliente.TotalGasto:C}";
            }
            catch (Exception ex)
            {
                InsightCrescimentoTextBlock.Text = $"Não foi possível carregar histórico de OS:\n{ex.Message}";
            }
        }

        private static string Escapar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Replace(';', ',').Trim();
        }

        private void NovoClienteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CLIENTES_CRIAR", "Voce nao possui permissao para cadastrar clientes."))
                return;

            var window = new NovoClienteWindow();
            WindowOwnerHelper.ConfigureOwner(window, this);

            if (window.ShowDialog() == true)
            {
                CarregarClientes();
            }
        }

        private void VisualizarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            var cliente = ObterClienteDoBotao(sender as Button);
            if (cliente == null)
                return;

            var clienteCompleto = App.Repositories.Clientes.ObterPorId(cliente.Id) ?? cliente;
            var window = new VisualizarClienteWindow(clienteCompleto);
            WindowOwnerHelper.ConfigureOwner(window, this);
            window.ShowDialog();
        }

        private void EditarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CLIENTES_EDITAR", "Voce nao possui permissao para editar clientes."))
                return;

            var cliente = ObterClienteDoBotao(sender as Button);
            if (cliente == null)
                return;

            var clienteCompleto = App.Repositories.Clientes.ObterPorId(cliente.Id) ?? cliente;
            var window = new EditarClienteWindow(clienteCompleto);
            WindowOwnerHelper.ConfigureOwner(window, this);

            if (window.ShowDialog() == true)
            {
                CarregarClientes();
            }
        }

        private void HistoricoClienteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cliente = ObterClienteDoBotao(sender as Button);
                if (cliente == null)
                    return;

                var clienteCompleto = App.Repositories.Clientes.ObterPorId(cliente.Id) ?? cliente;
                var historicoWindow = new HistoricoClienteWindow(clienteCompleto);
                WindowOwnerHelper.ConfigureOwner(historicoWindow, this);
                historicoWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao abrir historico:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExcluirClienteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CLIENTES_EXCLUIR", "Voce nao possui permissao para excluir clientes."))
                return;

            var cliente = ObterClienteDoBotao(sender as Button);
            if (cliente == null)
                return;

            if (!CriticalActionDialogService.ConfirmarExclusao(
                Window.GetWindow(this),
                "cliente",
                cliente.Nome,
                $"Telefone principal: {(string.IsNullOrWhiteSpace(cliente.Telefone) ? cliente.WhatsApp : cliente.Telefone)}\nDocumento: {cliente.Documento}\nVeiculos vinculados: {cliente.Veiculos.Count}",
                "O cadastro do cliente e os veiculos vinculados serao removidos do banco local. Esta acao nao possui desfazer automatico."))
                return;

            try
            {
                App.Repositories.Clientes.Excluir(cliente.Id);
                ClienteMediaService.DeleteManagedImageIfOwned(cliente.ImagemUrl);
                App.Audit.RegistrarAcaoCritica(
                    "Clientes",
                    "ExcluirCliente",
                    "Cliente",
                    cliente.Id.ToString(),
                    $"Nome={cliente.Nome}; Telefone={cliente.Telefone}");
                CarregarClientes();

                MessageBox.Show(
                    UiText.T("ClientRemoved"),
                    UiText.T("Success"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Nao foi possivel excluir o cliente:\n{ex.Message}",
                    "Exclusao bloqueada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ExportarClientesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CLIENTES_EXPORTAR", "Voce nao possui permissao para exportar dados de clientes."))
            {
                App.Audit.RegistrarAcaoCritica(
                    "Clientes",
                    "ExportarClientesNegado",
                    "Cliente",
                    "Exportacao",
                    $"Usuario={App.Session.UserName}; Perfil={App.Session.AccessProfile}");
                return;
            }

            try
            {
                var caminho = Path.Combine(
                    App.RuntimeAppDataPath,
                    "Exports",
                    $"clientes_{DateTime.Now:yyyyMMddHHmmss}.csv");

                Directory.CreateDirectory(Path.GetDirectoryName(caminho)!);

                var linhas = new List<string>
                {
                    "Nome;TipoPessoa;Documento;RG_IE;Contato;WhatsApp;Email;UltimaVisita;TotalGasto;TotalServicos;Veiculos;Status;LGPD;ContatoWhatsApp"
                };

                linhas.AddRange(_viewModel.AllClientes.Select(item =>
                    string.Join(";",
                        Escapar(item.Cliente.Nome),
                        Escapar(item.Cliente.TipoPessoaDescricao),
                        Escapar(item.Cliente.Documento),
                        Escapar(item.Cliente.RG),
                        Escapar(string.IsNullOrWhiteSpace(item.Cliente.Telefone) ? item.Cliente.WhatsApp : item.Cliente.Telefone),
                        Escapar(item.Cliente.WhatsApp),
                        Escapar(item.Cliente.Email),
                        Escapar(item.Cliente.UltimaVisita?.ToString("dd/MM/yyyy") ?? string.Empty),
                        item.Cliente.TotalGasto.ToString("F2", CultureInfo.InvariantCulture),
                        item.Cliente.TotalServicos.ToString(CultureInfo.InvariantCulture),
                        item.Cliente.Veiculos.Count.ToString(CultureInfo.InvariantCulture),
                        Escapar(item.Cliente.Ativo ? "Ativo" : "Inativo"),
                        Escapar(item.Cliente.ConsentimentoLGPD ? "LGPD registrado" : "LGPD pendente"),
                        Escapar(item.Cliente.AutorizaContatoWhatsApp ? "WhatsApp autorizado" : "WhatsApp nao autorizado"))));

                File.WriteAllLines(caminho, linhas, Encoding.UTF8);

                App.Audit.RegistrarAcaoCritica(
                    "Clientes",
                    "ExportarClientes",
                    "Cliente",
                    "Exportacao",
                    $"Arquivo={caminho}; Registros={_viewModel.AllClientes.Count}; Usuario={App.Session.UserName}");

                WindowInteractionHelper.ShowMessage(
                    $"Exportacao concluida em:\n{caminho}",
                    "Exportacao de clientes",
                    MessageBoxImage.Information,
                    "Clientes");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao exportar clientes:\n{ex.Message}",
                    "Exportacao",
                    MessageBoxImage.Error,
                    "Clientes",
                    ex);
            }
        }

        private void LimparFiltrosClientesButton_Click(object sender, RoutedEventArgs e)
        {
            BuscaClienteTextBox.Text = string.Empty;
            FiltroStatusComboBox.SelectedIndex = 0;
            AplicarFiltros();
        }

        private void BuscaClienteTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void FiltroStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            AplicarFiltros();
        }

        private void ClientesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarEstadoAcoesRapidas();
            AtualizarPainelLateral();
        }

        private void RestaurarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CLIENTES_EDITAR", "Voce nao possui permissao para restaurar clientes."))
                return;

            var cliente = ObterClienteDoBotao(sender as Button);
            if (cliente == null)
                return;

            try
            {
                var ok = App.Repositories.Clientes.Restaurar(cliente.Id);
                CarregarClientes();
                MessageBox.Show(
                    ok ? "Cliente restaurado (soft delete revertido)." : "Cliente nao estava marcado como excluido ou nao foi encontrado.",
                    "LGPD - Restaurar",
                    MessageBoxButton.OK,
                    ok ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Nao foi possivel restaurar:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void WhatsAppClienteButton_Click(object sender, RoutedEventArgs e)
        {
            var cliente = ObterClienteSelecionado();
            if (cliente == null)
            {
                return;
            }

            var contatoAtual = string.IsNullOrWhiteSpace(cliente.WhatsApp)
                ? cliente.Telefone
                : cliente.WhatsApp;

            if (!CadastroValidationHelper.TryObterTelefoneWhatsApp(contatoAtual, out var telefone))
            {
                MessageBox.Show(
                    UiText.T("ClientNoWhatsApp"),
                    UiText.T("ContactMissing"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!cliente.ConsentimentoLGPD || !cliente.AutorizaContatoWhatsApp)
            {
                var confirmado = CriticalActionDialogService.ConfirmarAcao(
                    Window.GetWindow(this),
                    new CriticalActionRequest
                    {
                        WindowTitle = "Contato LGPD pendente",
                        Header = "Revise o consentimento antes do contato",
                        Summary = $"O cliente '{cliente.Nome}' nao possui consentimento completo para contato por WhatsApp.",
                        Details = $"LGPD: {(cliente.ConsentimentoLGPD ? "registrado" : "pendente")}\nWhatsApp: {(cliente.AutorizaContatoWhatsApp ? "autorizado" : "nao autorizado")}",
                        Impact = "Confirme somente se este contato for necessario para atendimento em andamento ou se o consentimento foi validado fora do sistema.",
                        Keyword = "CONTATAR",
                        ConfirmButtonText = "Abrir WhatsApp"
                    });

                if (!confirmado)
                {
                    return;
                }
            }

            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "AbrirWhatsAppCliente",
                "Cliente",
                cliente.Id.ToString(),
                $"Nome={cliente.Nome}; LGPD={cliente.ConsentimentoLGPD}; WhatsAppAutorizado={cliente.AutorizaContatoWhatsApp}");

            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    $"WhatsApp do cliente validado em automacao para o telefone {telefone}.",
                    "Clientes");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://wa.me/{telefone}",
                UseShellExecute = true
            });
        }

        private void NovaOsClienteButton_Click(object sender, RoutedEventArgs e)
        {
            var cliente = ObterClienteSelecionadoCompleto();
            if (cliente == null)
            {
                return;
            }

            var janela = new OrdemServicoWindow(App.Database, null, cliente);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "AtalhoNovaOsCliente",
                "Cliente",
                cliente.Id.ToString(),
                $"Nome={cliente.Nome}; Origem=Clientes");

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "nova OS por atalho de cliente");
                return;
            }

            janela.ShowDialog();
        }

        private void NovoOrcamentoClienteButton_Click(object sender, RoutedEventArgs e)
        {
            var cliente = ObterClienteSelecionadoCompleto();
            if (cliente == null)
            {
                return;
            }

            var janela = new NovoOrcamentoWindow(cliente);
            WindowOwnerHelper.ConfigureOwner(janela, this);

            App.Audit.RegistrarAcaoCritica(
                "Clientes",
                "AtalhoNovoOrcamentoCliente",
                "Cliente",
                cliente.Id.ToString(),
                $"Nome={cliente.Nome}; Origem=Clientes");

            if (App.IsAutomatedTestMode)
            {
                ValidarJanelaEmAutomacao(janela, "novo orcamento por atalho de cliente");
                return;
            }

            if (janela.ShowDialog() == true)
            {
                CarregarClientes();
            }
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
                return true;

            MessageBox.Show(mensagem, UiText.T("AccessDenied"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private static Cliente? ObterClienteDoBotao(Button? button)
        {
            return button?.DataContext is ClienteListItemViewModel item ? item.Cliente : null;
        }

        private Cliente? ObterClienteSelecionado()
        {
            return ClientesDataGrid?.SelectedItem is ClienteListItemViewModel item ? item.Cliente : null;
        }

        private Cliente? ObterClienteSelecionadoCompleto()
        {
            var cliente = ObterClienteSelecionado();
            return cliente == null ? null : App.Repositories.Clientes.ObterPorId(cliente.Id) ?? cliente;
        }

        private void AtualizarEstadoAcoesRapidas()
        {
            var temSelecao = ObterClienteSelecionado() != null;

            if (WhatsAppClienteButton != null)
            {
                WhatsAppClienteButton.IsEnabled = temSelecao;
                WhatsAppClienteButton.ToolTip = temSelecao
                    ? "Abre WhatsApp do cliente selecionado com alerta LGPD quando necessario."
                    : "Selecione um cliente para abrir WhatsApp.";
            }

            if (NovaOsClienteButton != null)
            {
                NovaOsClienteButton.IsEnabled = temSelecao;
                NovaOsClienteButton.ToolTip = temSelecao
                    ? "Abre uma ordem de servico pre-vinculada ao cliente selecionado."
                    : "Selecione um cliente para criar OS.";
            }

            if (NovoOrcamentoClienteButton != null)
            {
                NovoOrcamentoClienteButton.IsEnabled = temSelecao;
                NovoOrcamentoClienteButton.ToolTip = temSelecao
                    ? "Abre um orcamento pre-vinculado ao cliente selecionado."
                    : "Selecione um cliente para criar orcamento.";
            }
        }

        private static void ValidarJanelaEmAutomacao(Window janela, string contexto)
        {
            try
            {
                janela.ApplyTemplate();
                if (janela.Content is FrameworkElement content)
                {
                    content.ApplyTemplate();
                    content.Measure(new Size(1280, 720));
                    content.Arrange(new Rect(0, 0, 1280, 720));
                    content.UpdateLayout();
                }

                App.Logger.LogInfo($"Janela de {contexto} validada em automacao sem abrir modal bloqueante.", "Clientes");
            }
            finally
            {
                janela.Close();
            }
        }

        
    }
}
