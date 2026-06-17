using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Printing;
using System.IO;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrimoAutoEletrica.UserControls
{
    public partial class PDVControl : UserControl
    {
        private readonly DatabaseService _databaseService;
        private readonly VendaService _vendaService;
        private readonly CaixaService _caixaService;
        private readonly EstoqueOperationalService _estoqueOperationalService;
        private readonly VendaComprovanteService _comprovanteService;
        private readonly PrinterDiagnosticsService _printerDiagnosticsService;
        private readonly PDVViewModel _viewModel;
        private readonly PermissionService _permissionService;

        private List<Produto> _todosProdutos = new();
        private List<Cliente> _todosClientes = new();

        private DispatcherTimer? _timer;
        private bool _atalhosConfigurados;
        private bool _diagnosticoImpressaoRegistrado;
        private Window? _janelaAtalhos;
        private string _categoriaSelecionada = string.Empty;

        private static readonly List<VendaSuspensaPdv> VendasSuspensas = new();

        public PDVViewModel ViewModel => _viewModel;

        public PDVControl()
        {
            InitializeComponent();

            _databaseService = global::PrimoAutoEletrica.App.Database;
            _vendaService = new VendaService(_databaseService);
            _caixaService = new CaixaService(_databaseService);
            _estoqueOperationalService = new EstoqueOperationalService(_databaseService, App.Logger);
            _comprovanteService = new VendaComprovanteService();
            _printerDiagnosticsService = new PrinterDiagnosticsService();
            _viewModel = new PDVViewModel();
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);

            DataContext = _viewModel;

            Loaded += PDVControl_Loaded;
            Unloaded += PDVControl_Unloaded;
        }

        private void PDVControl_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarDadosIniciais();
            ConfigurarAtalhosTeclado();
            ConfigurarTimer();
            AtualizarEstadoCaixa();
            AtualizarFormaPagamentoSelecionada();
            AtualizarResumoVendasSuspensas();
            RegistrarDiagnosticoImpressao();
        }

        private void PDVControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();

            PreviewKeyDown -= OnPdvKeyDown;
            KeyDown -= OnPdvKeyDown;

            if (_janelaAtalhos != null)
            {
                _janelaAtalhos.PreviewKeyDown -= OnPdvKeyDown;
                _janelaAtalhos = null;
            }

            _atalhosConfigurados = false;
        }

        private void ConfigurarTimer()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(60)
                };

                _timer.Tick += (s, args) =>
                {
                    _viewModel.AtualizarHora();
                    AtualizarEstadoCaixa();
                };
            }

            _timer.Start();
        }

        private void ConfigurarAtalhosTeclado()
        {
            if (_atalhosConfigurados)
            {
                return;
            }

            Focusable = true;

            PreviewKeyDown -= OnPdvKeyDown;
            PreviewKeyDown += OnPdvKeyDown;

            KeyDown -= OnPdvKeyDown;
            KeyDown += OnPdvKeyDown;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var janela = Window.GetWindow(this);

                if (janela != null)
                {
                    _janelaAtalhos = janela;
                    _janelaAtalhos.PreviewKeyDown -= OnPdvKeyDown;
                    _janelaAtalhos.PreviewKeyDown += OnPdvKeyDown;
                }

                Focus();
            }), DispatcherPriority.ContextIdle);

            _atalhosConfigurados = true;
        }

        private void CarregarDadosIniciais()
        {
            try
            {
                _todosProdutos = App.Repositories.Produtos.ObterTodos();
                _estoqueOperationalService.EnriquecerProdutosComReservas(_todosProdutos);
                _todosClientes = App.Repositories.Clientes.ObterTodos();

                _viewModel.Produtos.Clear();
                _viewModel.Clientes.Clear();

                AtualizarProdutosVisiveis(_todosProdutos.Take(10));

                foreach (var cliente in _todosClientes.Take(5))
                {
                    _viewModel.Clientes.Add(cliente);
                }

                _viewModel.AtualizarTotal();
                AtualizarEstadoCaixa();

                if (_todosProdutos.Count == 0 && _todosClientes.Count == 0)
                {
                    ExibirMensagem(
                        "Nenhum produto ou cliente encontrado no banco de dados.\n\nCadastre produtos e clientes primeiro para usar o PDV.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Erro ao carregar dados: {ex.Message}\n\nStack Trace: {ex.StackTrace}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnPdvKeyDown(object sender, KeyEventArgs args)
        {
            if (args.Key == Key.F1)
            {
                AbrirJanelaSelecionarProduto();
                args.Handled = true;
            }
            else if (args.Key == Key.F2)
            {
                DescontoTextBox.Focus();
                DescontoTextBox.SelectAll();
                args.Handled = true;
            }
            else if (args.Key == Key.F5)
            {
                AbrirJanelaSelecionarCliente();
                args.Handled = true;
            }
            else if (args.Key == Key.F6)
            {
                PagamentoButton_Click(sender, new RoutedEventArgs());
                args.Handled = true;
            }
            else if (args.Key == Key.F9)
            {
                SuspenderVendaButton_Click(sender, new RoutedEventArgs());
                args.Handled = true;
            }
            else if (args.Key == Key.F10)
            {
                RetomarVendaSuspensaButton_Click(sender, new RoutedEventArgs());
                args.Handled = true;
            }
            else if (args.Key == Key.Delete)
            {
                RemoverItemSelecionado();
                args.Handled = true;
            }
            else if (args.Key == Key.Escape)
            {
                CancelarVendaButton_Click(sender, new RoutedEventArgs());
                args.Handled = true;
            }
        }

    }
}
