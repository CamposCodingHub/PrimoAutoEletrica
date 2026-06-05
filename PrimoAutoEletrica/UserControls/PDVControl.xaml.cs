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

        private void BuscarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            RealizarBuscaProduto();
        }

        private void AbrirSelecionarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirJanelaSelecionarProduto();
        }

        private void AbrirJanelaSelecionarProduto()
        {
            try
            {
                if (_todosProdutos.Count == 0)
                {
                    CarregarDadosIniciais();
                }

                if (_todosProdutos.Count == 0)
                {
                    ExibirMensagem(
                        "Nenhum produto encontrado no estoque.\n\nCadastre ou importe produtos antes de usar a seleção do PDV.",
                        "Selecionar produto",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    return;
                }

                var janela = new SelecionarProdutoPDVWindow(_todosProdutos);
                ConfigurarOwner(janela);

                if (janela.ShowDialog() == true && janela.ProdutoSelecionado != null)
                {
                    _viewModel.SelectedProduto = janela.ProdutoSelecionado;
                    AdicionarProdutoAoCarrinho(janela.ProdutoSelecionado);

                    if (!janela.FecharAposAdicionar)
                    {
                        Dispatcher.BeginInvoke(new Action(AbrirJanelaSelecionarProduto), DispatcherPriority.ContextIdle);
                    }
                }
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Erro ao abrir seleção de produtos:\n{ex.Message}",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BuscaProdutoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RealizarBuscaProduto();
        }

        private void BuscaProdutoTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RealizarBuscaProduto();
                e.Handled = true;
            }
        }

        private void RealizarBuscaProduto()
        {
            var termo = BuscaProdutoTextBox.Text.Trim().ToUpperInvariant();

            _viewModel.Produtos.Clear();

            IEnumerable<Produto> produtosFiltrados = _todosProdutos;

            if (!string.IsNullOrWhiteSpace(_categoriaSelecionada))
            {
                produtosFiltrados = produtosFiltrados
                    .Where(p => string.Equals(p.Categoria, _categoriaSelecionada, StringComparison.OrdinalIgnoreCase));
            }

            if (string.IsNullOrWhiteSpace(termo))
            {
                AtualizarProdutosVisiveis(produtosFiltrados.Take(10));
                return;
            }

            var resultados = produtosFiltrados
                .Where(p =>
                    TextoContem(p.Nome, termo) ||
                    TextoContem(p.Codigo, termo) ||
                    TextoContem(p.SKU, termo) ||
                    TextoContem(p.CodigoBarras, termo) ||
                    TextoContem(p.Categoria, termo) ||
                    TextoContem(p.Marca, termo))
                .Take(30)
                .ToList();

            AtualizarProdutosVisiveis(resultados);

            if (resultados.Count == 1)
            {
                _viewModel.SelectedProduto = resultados[0];
                AdicionarProdutoAoCarrinho(resultados[0]);
            }
            else if (resultados.Count > 1)
            {
                AbrirJanelaSelecionarProduto();
            }
            else
            {
                ExibirMensagem(
                    "Nenhum produto encontrado para a busca informada.",
                    "Busca de produto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void CategoriaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string categoria)
            {
                _categoriaSelecionada = categoria;

                BuscaProdutoTextBox.Clear();

                IEnumerable<Produto> produtosFiltrados = _todosProdutos;

                if (!string.IsNullOrWhiteSpace(_categoriaSelecionada))
                {
                    produtosFiltrados = produtosFiltrados
                        .Where(p => string.Equals(p.Categoria, _categoriaSelecionada, StringComparison.OrdinalIgnoreCase));
                }

                AtualizarProdutosVisiveis(produtosFiltrados.Take(30));
            }
        }

        private void FormaPagamentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string formaPagamento)
            {
                return;
            }

            _viewModel.FormaPagamentoSelecionada = formaPagamento;
            _viewModel.PagamentoMistoResumo = string.Equals(formaPagamento, "Misto", StringComparison.OrdinalIgnoreCase)
                ? "Pagamento misto: rateio sera conferido ao finalizar."
                : string.Empty;

            AtualizarFormaPagamentoSelecionada();
        }

        private void AdicionarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: Produto produto })
            {
                _viewModel.SelectedProduto = produto;
                AdicionarProdutoAoCarrinho(produto);
            }
        }

        private void ProdutosListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedProduto != null)
            {
                AdicionarProdutoAoCarrinho(_viewModel.SelectedProduto);
            }
        }

        private void AdicionarProdutoAoCarrinho(Produto produto)
        {
            var disponibilidade = ObterDisponibilidadeOperacional(produto);

            if (disponibilidade <= 0)
            {
                ExibirMensagem(
                    "Produto sem disponibilidade operacional no momento.",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var itemExistente = _viewModel.Carrinho
                .FirstOrDefault(i => i.Produto != null && i.Produto.Id == produto.Id);

            if (itemExistente != null)
            {
                if (itemExistente.Quantidade + 1 > disponibilidade)
                {
                    ExibirMensagem(
                        $"Disponibilidade operacional insuficiente. Restante: {Math.Max(0, disponibilidade - itemExistente.Quantidade)}.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                itemExistente.Quantidade++;
            }
            else
            {
                _viewModel.Carrinho.Add(new ItemVenda
                {
                    Produto = produto,
                    ProdutoId = produto.Id,
                    Tipo = "Produto",
                    Descricao = produto.Nome,
                    Quantidade = 1,
                    PrecoUnitario = produto.PrecoVenda,
                    CustoUnitario = produto.PrecoCompra,
                    Desconto = 0
                });
            }

            AtualizarGridCarrinho();

            BuscaProdutoTextBox.Clear();
            _viewModel.Produtos.Clear();
        }

        private void BuscarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirJanelaSelecionarCliente();
        }

        private void AbrirSelecionarClienteButton_Click(object sender, RoutedEventArgs e)
        {
            AbrirJanelaSelecionarCliente();
        }

        private void AbrirJanelaSelecionarCliente()
        {
            try
            {
                if (_todosClientes.Count == 0)
                {
                    CarregarDadosIniciais();
                }

                if (_todosClientes.Count == 0)
                {
                    ExibirMensagem(
                        "Nenhum cliente encontrado no cadastro.\n\nA venda pode continuar como Consumidor final.",
                        "Selecionar cliente",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    _viewModel.ClienteSelecionado = null;
                    return;
                }

                var janela = new SelecionarClientePDVWindow(_todosClientes);
                ConfigurarOwner(janela);

                if (janela.ShowDialog() == true)
                {
                    _viewModel.ClienteSelecionado = janela.UsarConsumidorFinal
                        ? null
                        : janela.ClienteSelecionado;

                    BuscaClienteTextBox.Clear();

                    if (_viewModel.ClienteSelecionado != null)
                    {
                        ExibirMensagem(
                            $"Cliente selecionado: {_viewModel.ClienteSelecionado.Nome}",
                            "Cliente",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Erro ao abrir seleção de clientes:\n{ex.Message}",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BuscaClienteTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RealizarBuscaCliente();
                e.Handled = true;
            }
        }

        private void RealizarBuscaCliente()
        {
            var termo = BuscaClienteTextBox.Text.Trim().ToUpperInvariant();

            _viewModel.Clientes.Clear();

            if (string.IsNullOrWhiteSpace(termo))
            {
                foreach (var cliente in _todosClientes.Take(5))
                {
                    _viewModel.Clientes.Add(cliente);
                }

                return;
            }

            var resultados = _todosClientes
                .Where(c =>
                {
                    var termoDocumento = CadastroValidationHelper.NormalizarDocumento(termo);
                    var termoTelefone = CadastroValidationHelper.NormalizarTelefone(termo);

                    return TextoContem(c.Nome, termo) ||
                           TextoContem(c.Documento, termo) ||
                           (!string.IsNullOrWhiteSpace(termoDocumento) &&
                            CadastroValidationHelper.NormalizarDocumento(c.CPF).Contains(termoDocumento, StringComparison.Ordinal)) ||
                           (!string.IsNullOrWhiteSpace(termoTelefone) &&
                            CadastroValidationHelper.NormalizarTelefone(c.Telefone).Contains(termoTelefone, StringComparison.Ordinal)) ||
                           (!string.IsNullOrWhiteSpace(termoTelefone) &&
                            CadastroValidationHelper.NormalizarTelefone(c.WhatsApp).Contains(termoTelefone, StringComparison.Ordinal)) ||
                           TextoContem(c.Email, termo);
                })
                .Take(10)
                .ToList();

            foreach (var cliente in resultados)
            {
                _viewModel.Clientes.Add(cliente);
            }
        }

        private void ClientesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.ClienteSelecionado != null)
            {
                ExibirMensagem(
                    $"Cliente selecionado: {_viewModel.ClienteSelecionado.Nome}",
                    "Cliente",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void VisualizarItemCarrinhoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not ItemVenda item)
            {
                return;
            }

            var produto = item.Produto;

            var detalhes =
                $"Descrição: {item.NomeExibicao}\n" +
                $"Tipo: {item.Tipo}\n" +
                $"Quantidade: {item.Quantidade}\n" +
                $"Preço unitário: {item.PrecoUnitario:C}\n" +
                $"Custo unitário: {item.CustoUnitario:C}\n" +
                $"Desconto do item: {item.Desconto:C}\n" +
                $"Subtotal: {item.Subtotal:C}";

            if (produto != null)
            {
                detalhes +=
                    "\n\n--- Produto ---\n" +
                    $"Código: {produto.Codigo}\n" +
                    $"SKU: {produto.SKU}\n" +
                    $"Código de barras: {produto.CodigoBarras}\n" +
                    $"Categoria: {produto.Categoria}\n" +
                    $"Marca: {produto.Marca}\n" +
                    $"Fornecedor: {produto.Fornecedor}\n" +
                    $"Estoque disponível: {produto.QuantidadeDisponivel}\n" +
                    $"Preço venda atual: {produto.PrecoVenda:C}";
            }

            ExibirMensagem(
                detalhes,
                "Detalhes do item",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RemoverItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ItemVenda item)
            {
                RemoverItemDoCarrinho(item);
            }
        }

        private void AumentarQuantidadeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ItemVenda item)
            {
                if (!item.UsaEstoque || item.Produto == null)
                {
                    item.Quantidade++;
                    AtualizarGridCarrinho();
                    return;
                }

                var disponibilidade = ObterDisponibilidadeOperacional(item.Produto);

                if (item.Quantidade + 1 > disponibilidade)
                {
                    ExibirMensagem(
                        $"Disponibilidade operacional insuficiente. Restante: {Math.Max(0, disponibilidade - item.Quantidade)}.",
                        "Aviso",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                item.Quantidade++;
                AtualizarGridCarrinho();
            }
        }

        private void DiminuirQuantidadeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ItemVenda item)
            {
                if (item.Quantidade > 1)
                {
                    item.Quantidade--;
                    AtualizarGridCarrinho();
                    return;
                }

                RemoverItemDoCarrinho(item);
            }
        }

        private void RemoverItemSelecionado()
        {
            if (CarrinhoListView.SelectedItem is ItemVenda item)
            {
                RemoverItemDoCarrinho(item);
            }
        }

        private void RemoverItemDoCarrinho(ItemVenda item)
        {
            var resultado = ExibirMensagem(
                $"Deseja remover {item.NomeExibicao} do carrinho?",
                "Remover Item",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                _viewModel.Carrinho.Remove(item);
                AtualizarGridCarrinho();
            }
        }

        private void AplicarDescontoButton_Click(object sender, RoutedEventArgs e)
        {
            AplicarDesconto();
        }

        private void DescontoTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AplicarDesconto();
                e.Handled = true;
            }
        }

        private void AplicarDesconto()
        {
            if (!ValidarPermissao("PDV_APLICAR_DESCONTO", "Voce nao possui permissao para aplicar descontos no PDV."))
            {
                return;
            }

            var descontoAnterior = _viewModel.DescontoGeral;

            if (TryParseDecimalFlexible(DescontoTextBox.Text, out var desconto))
            {
                try
                {
                    ComercialValidationHelper.GarantirValorMaiorOuIgualZero(desconto, "o desconto");
                    ComercialValidationHelper.GarantirDescontoValido(desconto, _viewModel.Subtotal, "O desconto");
                }
                catch (InvalidOperationException ex)
                {
                    ExibirMensagem(ex.Message, "Desconto", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _viewModel.DescontoGeral = desconto;
                _viewModel.AtualizarTotal();

                global::PrimoAutoEletrica.App.Audit.Registrar(
                    categoria: "PDV",
                    acao: "DescontoAplicado",
                    entidade: "Carrinho",
                    entidadeId: global::PrimoAutoEletrica.App.Session.SessionId.ToString(),
                    detalhes: $"Subtotal={_viewModel.Subtotal:C}; Total={_viewModel.Total:C}",
                    valorAnterior: descontoAnterior.ToString("F2"),
                    valorNovo: desconto.ToString("F2"));

                ExibirMensagem(
                    $"Desconto de R$ {desconto:F2} aplicado!",
                    "Desconto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            ExibirMensagem(
                "Valor de desconto invalido!",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        private static bool TextoContem(string? origem, string termo)
        {
            return !string.IsNullOrWhiteSpace(origem)
                && origem.ToUpperInvariant().Contains(termo);
        }

        private static bool TryParseDecimalFlexible(string? text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private void PagamentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Carrinho.Count == 0)
            {
                ExibirMensagem(
                    "Carrinho vazio!",
                    "Aviso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var sessaoCaixa = GarantirCaixaAbertoParaVenda();

            if (sessaoCaixa == null)
            {
                return;
            }

            var total = _viewModel.Total;

            if (total <= 0)
            {
                ExibirMensagem(
                    "O total da venda precisa ser maior que zero.",
                    "Pagamento",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            var formaPagamento = ResolverFormaPagamentoFinal(total);

            if (string.IsNullOrWhiteSpace(formaPagamento))
            {
                return;
            }

            var confirmarPagamento = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Finalizar venda",
                    Header = "Confirmacao de pagamento",
                    Summary = "Voce esta prestes a concluir a venda atual do PDV.",
                    Details = $"Forma de pagamento: {formaPagamento}\nItens: {_viewModel.Carrinho.Count}\nCliente: {_viewModel.ClienteSelecionado?.Nome ?? "Consumidor final"}\nTotal a pagar: {total:C}",
                    Impact = "A venda sera gravada com atualizacao de estoque, caixa e financeiro desta sessao.",
                    Keyword = "PAGAR",
                    ConfirmButtonText = "Finalizar venda"
                });

            if (confirmarPagamento)
            {
                FinalizarVenda(total, sessaoCaixa, formaPagamento);
            }
        }

        private string? ResolverFormaPagamentoFinal(decimal total)
        {
            if (!string.Equals(_viewModel.FormaPagamentoSelecionada, "Misto", StringComparison.OrdinalIgnoreCase))
            {
                return _viewModel.FormaPagamentoSelecionada;
            }

            if (!ValidarPermissao("PDV_PAGAMENTO_MISTO", "Voce nao possui permissao para finalizar vendas com pagamento misto."))
            {
                return null;
            }

            if (App.IsAutomatedTestMode)
            {
                var primeiraParte = decimal.Round(total / 2m, 2);
                var segundaParte = total - primeiraParte;
                var resumoAutomacao = $"Misto: Dinheiro {primeiraParte:C} | PIX {segundaParte:C}";

                _viewModel.PagamentoMistoResumo = resumoAutomacao;

                App.Logger.LogInfo(
                    $"Pagamento misto validado em automacao: {resumoAutomacao}.",
                    "PDV");

                return resumoAutomacao;
            }

            var dialog = new PagamentoMistoWindow(total);
            WindowOwnerHelper.ConfigureOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return null;
            }

            _viewModel.PagamentoMistoResumo = dialog.ResumoPagamento;
            return dialog.ResumoPagamento;
        }

        private void FinalizarVenda(decimal total, CaixaSessaoOperacional sessaoCaixa, string formaPagamento)
        {
            if (!ValidarPermissao("PDV_REGISTRAR_VENDA", "Voce nao possui permissao para finalizar vendas no PDV."))
            {
                return;
            }

            try
            {
                _viewModel.AtualizarTotal();

                var descontoAplicado = Math.Min(_viewModel.DescontoGeral, _viewModel.Subtotal);

                if (descontoAplicado != _viewModel.DescontoGeral)
                {
                    _viewModel.DescontoGeral = descontoAplicado;
                    _viewModel.AtualizarTotal();
                }

                total = _viewModel.Total;

                if (total <= 0)
                {
                    ExibirMensagem(
                        "O total da venda precisa ser maior que zero.",
                        "Pagamento",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                var venda = new Venda
                {
                    Data = DateTime.Now,
                    Cliente = _viewModel.ClienteSelecionado,
                    Itens = new List<ItemVenda>(_viewModel.Carrinho),
                    Total = total,
                    Desconto = _viewModel.DescontoGeral,
                    FormaPagamento = formaPagamento,
                    Usuario = global::PrimoAutoEletrica.App.Session.UserName,
                    CaixaSessaoId = sessaoCaixa.Id
                };

                _vendaService.RegistrarVenda(venda);
                _viewModel.UltimaVendaFinalizadaId = venda.Id;

                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "PDV",
                    "VendaFinalizadaTela",
                    "Venda",
                    venda.Id.ToString(),
                    $"Total={total:C}; Itens={venda.Itens.Count}; FormaPagamento={venda.FormaPagamento}; Caixa={sessaoCaixa.NumeroCaixa}");

                ExibirMensagem(
                    $"Venda realizada com sucesso!\nForma de pagamento: {venda.FormaPagamento}\nTotal: R$ {total:F2}",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                LimparCarrinho();
                CarregarDadosIniciais();
                AtualizarEstadoCaixa();
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao finalizar venda no PDV.", ex);

                ExibirMensagem(
                    $"Erro ao finalizar venda: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelarVendaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Carrinho.Count == 0)
            {
                return;
            }

            if (!ValidarPermissao("PDV_CANCELAR_VENDA", "Voce nao possui permissao para cancelar a venda atual."))
            {
                return;
            }

            if (CriticalActionDialogService.ConfirmarCancelamento(
                Window.GetWindow(this),
                "venda",
                "venda atual",
                $"Itens no carrinho: {_viewModel.Carrinho.Count}\nTotal atual: {_viewModel.Total:C}\nCliente selecionado: {_viewModel.ClienteSelecionado?.Nome ?? "Nao informado"}",
                "O carrinho, o desconto aplicado e o cliente selecionado serao descartados desta sessao do PDV."))
            {
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "PDV",
                    "VendaCanceladaTela",
                    "Carrinho",
                    Guid.NewGuid().ToString(),
                    $"Itens={_viewModel.Carrinho.Count}; Total={_viewModel.Total:C}");

                LimparCarrinho();
            }
        }

        private void SuspenderVendaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Carrinho.Count == 0)
            {
                ExibirMensagem(
                    "Nao ha itens no carrinho para suspender.",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (!ValidarPermissao("PDV_SUSPENDER_VENDA", "Voce nao possui permissao para suspender vendas no PDV."))
            {
                return;
            }

            var suspensa = new VendaSuspensaPdv(
                Guid.NewGuid(),
                DateTime.Now,
                _viewModel.ClienteSelecionado,
                _viewModel.Carrinho.Select(CloneItemVenda).ToList(),
                _viewModel.DescontoGeral,
                _viewModel.FormaPagamentoSelecionada,
                _viewModel.PagamentoMistoResumo,
                App.Session.UserName);

            VendasSuspensas.Add(suspensa);

            App.Audit.RegistrarAcaoCritica(
                "PDV",
                "VendaSuspensa",
                "Carrinho",
                suspensa.Id.ToString(),
                $"Itens={suspensa.Itens.Count}; Total={suspensa.Total:C}; Cliente={suspensa.Cliente?.Nome ?? "Consumidor final"}; Operador={suspensa.Operador}");

            LimparCarrinho();
            AtualizarResumoVendasSuspensas();

            ExibirMensagem(
                $"Venda suspensa com sucesso.\nTotal: {suspensa.Total:C}",
                "PDV",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void RetomarVendaSuspensaButton_Click(object sender, RoutedEventArgs e)
        {
            if (VendasSuspensas.Count == 0)
            {
                ExibirMensagem(
                    "Nao ha vendas suspensas para retomar.",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (!ValidarPermissao("PDV_RETOMAR_VENDA", "Voce nao possui permissao para retomar vendas suspensas no PDV."))
            {
                return;
            }

            if (_viewModel.Carrinho.Count > 0 &&
                !CriticalActionDialogService.ConfirmarAcao(
                    Window.GetWindow(this),
                    new CriticalActionRequest
                    {
                        WindowTitle = "Retomar venda suspensa",
                        Header = "Substituir carrinho atual",
                        Summary = "Existe uma venda em andamento no PDV.",
                        Details = $"Itens atuais: {_viewModel.Carrinho.Count}\nTotal atual: {_viewModel.Total:C}",
                        Impact = "O carrinho atual sera descartado para retomar a venda suspensa selecionada.",
                        Keyword = "RETOMAR",
                        ConfirmButtonText = "Retomar suspensa"
                    }))
            {
                return;
            }

            var suspensa = VendasSuspensas
                .OrderByDescending(venda => venda.DataSuspensao)
                .First();

            VendasSuspensas.Remove(suspensa);

            _viewModel.Carrinho.Clear();

            foreach (var item in suspensa.Itens.Select(CloneItemVenda))
            {
                _viewModel.Carrinho.Add(item);
            }

            _viewModel.ClienteSelecionado = suspensa.Cliente;
            _viewModel.DescontoGeral = suspensa.DescontoGeral;
            _viewModel.FormaPagamentoSelecionada = suspensa.FormaPagamento;
            _viewModel.PagamentoMistoResumo = suspensa.PagamentoMistoResumo;

            AtualizarGridCarrinho();
            AtualizarFormaPagamentoSelecionada();
            AtualizarResumoVendasSuspensas();

            App.Audit.RegistrarAcaoCritica(
                "PDV",
                "VendaSuspensaRetomada",
                "Carrinho",
                suspensa.Id.ToString(),
                $"Itens={suspensa.Itens.Count}; Total={suspensa.Total:C}; Cliente={suspensa.Cliente?.Nome ?? "Consumidor final"}; Operador={App.Session.UserName}");

            ExibirMensagem(
                $"Venda suspensa retomada.\nTotal: {_viewModel.Total:C}",
                "PDV",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void AbrirCaixaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CAIXA_ABRIR", "Voce nao possui permissao para abrir o caixa."))
            {
                return;
            }

            if (_caixaService.ObterSessaoAbertaAtual() != null)
            {
                ExibirMensagem(
                    "Ja existe um caixa aberto para este operador.",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AtualizarEstadoCaixa();
                return;
            }

            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Abrir caixa",
                Header = "Abertura de caixa",
                Subheader = "Informe o valor inicial que entra no caixa nesta sessao operacional.",
                ValorLabel = "Valor de abertura",
                ConfirmButtonText = "Abrir caixa",
                PermitirZero = true
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var sessao = _caixaService.AbrirCaixa(dialog.ValorInformado, dialog.ObservacoesInformadas);

                AtualizarEstadoCaixa();

                ExibirMensagem(
                    $"Caixa {sessao.NumeroCaixa} aberto com sucesso.\nSaldo inicial: {sessao.ValorAbertura:C}",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SuprimentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CAIXA_SUPRIMENTO", "Voce nao possui permissao para registrar suprimento."))
            {
                return;
            }

            if (!GarantirSessaoCaixaAberta())
            {
                return;
            }

            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Suprimento de caixa",
                Header = "Suprimento operacional",
                Subheader = "Registre o valor que esta entrando no caixa fora do fluxo de venda.",
                ValorLabel = "Valor do suprimento",
                ConfirmButtonText = "Registrar suprimento"
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var sessao = _caixaService.RegistrarSuprimento(
                    dialog.ValorInformado,
                    dialog.ObservacoesInformadas,
                    _viewModel.FormaPagamentoSelecionada);

                AtualizarEstadoCaixa();

                ExibirMensagem(
                    $"Suprimento registrado.\nSaldo esperado atual: {sessao.ValorEsperado:C}",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SangriaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CAIXA_SANGRIA", "Voce nao possui permissao para registrar sangria."))
            {
                return;
            }

            var sessao = _caixaService.ObterSessaoAbertaAtual();

            if (sessao == null)
            {
                ExibirMensagem(
                    "Abra o caixa antes de registrar uma sangria.",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                AtualizarEstadoCaixa();
                return;
            }

            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Sangria de caixa",
                Header = "Sangria operacional",
                Subheader = $"Saldo esperado atual: {sessao.ValorEsperado:C}\nInforme o valor que sera retirado do caixa.",
                ValorLabel = "Valor da sangria",
                ConfirmButtonText = "Registrar sangria",
                ObservacoesObrigatorias = true
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var sessaoAtualizada = _caixaService.RegistrarSangria(
                    dialog.ValorInformado,
                    dialog.ObservacoesInformadas,
                    _viewModel.FormaPagamentoSelecionada);

                AtualizarEstadoCaixa();

                ExibirMensagem(
                    $"Sangria registrada.\nSaldo esperado atual: {sessaoAtualizada.ValorEsperado:C}",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void FecharCaixaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("CAIXA_FECHAR", "Voce nao possui permissao para fechar o caixa."))
            {
                return;
            }

            var sessao = _caixaService.ObterSessaoAbertaAtual();

            if (sessao == null)
            {
                ExibirMensagem(
                    "Nenhum caixa aberto foi encontrado para fechamento.",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AtualizarEstadoCaixa();
                return;
            }

            var dialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Fechar caixa",
                Header = "Fechamento de caixa",
                Subheader = $"Valor esperado no sistema: {sessao.ValorEsperado:C}\nInforme o valor contado em caixa.",
                ValorLabel = "Valor contado",
                ConfirmButtonText = "Avancar para fechamento",
                PermitirZero = true,
                ValorInicial = sessao.ValorEsperado
            });

            ConfigurarOwner(dialog);

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var diferenca = dialog.ValorInformado - sessao.ValorEsperado;

            var confirmar = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Fechar caixa",
                    Header = "Confirmacao de fechamento",
                    Summary = $"Voce esta prestes a fechar o caixa {sessao.NumeroCaixa}.",
                    Details = $"Valor esperado: {sessao.ValorEsperado:C}\nValor informado: {dialog.ValorInformado:C}\nDiferenca: {diferenca:C}\nVendas da sessao: {sessao.QuantidadeVendas}",
                    Impact = "Depois do fechamento, o PDV volta a bloquear novas vendas ate uma nova abertura de caixa.",
                    Keyword = "FECHAR",
                    ConfirmButtonText = "Fechar caixa agora"
                });

            if (!confirmar)
            {
                return;
            }

            try
            {
                var sessaoFechada = _caixaService.FecharCaixa(dialog.ValorInformado, dialog.ObservacoesInformadas);

                AtualizarEstadoCaixa();

                ExibirMensagem(
                    $"Caixa fechado com sucesso.\nValor esperado: {sessaoFechada.ValorEsperado:C}\nValor informado: {sessaoFechada.ValorInformadoFechamento.GetValueOrDefault():C}\nDiferenca: {sessaoFechada.DiferencaFechamento:C}",
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "Caixa",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ReimprimirUltimaVendaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("PDV_REIMPRIMIR", "Voce nao possui permissao para reimprimir comprovantes do PDV."))
            {
                return;
            }

            var venda = SelecionarVendaOperacional(
                titulo: "Reimprimir comprovante",
                subtitulo: "Escolha uma venda recente para reimpressao do comprovante operacional.",
                textoConfirmacao: "Reimprimir venda",
                incluirCanceladas: false,
                filtro: item => !string.Equals(item.Status, "Cancelada", StringComparison.OrdinalIgnoreCase));

            if (venda == null)
            {
                return;
            }

            try
            {
                var snapshot = _printerDiagnosticsService.CaptureSnapshot();

                if (global::PrimoAutoEletrica.App.IsAutomatedTestMode)
                {
                    var documentoAutomacao = _comprovanteService.CriarDocumento(venda);
                    _ = documentoAutomacao.Blocks.Count;

                    global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                        "PDV",
                        "VendaReimpressa",
                        "Venda",
                        venda.Id.ToString(),
                        $"FormaPagamento={venda.FormaPagamento}; Total={venda.Total:C}; Modo=AutomatedTest; Impressoras={snapshot.BuildSummary()}");

                    return;
                }

                if (!snapshot.HasInstalledPrinters)
                {
                    // Fallback automatizado: salvar comprovante em arquivo XPS para auditoria quando nao houver impressora
                    try
                    {
                        var documentoFallback = _comprovanteService.CriarDocumento(venda);
                        var fallbackDir = Path.Combine(App.RuntimeAppDataPath, "Reimpressao");
                        Directory.CreateDirectory(fallbackDir);
                        var fileName = $"reimpressao-{venda.Id}-{DateTime.Now:yyyyMMddHHmmss}.xaml";
                        var filePath = Path.Combine(fallbackDir, fileName);

                        var xaml = XamlWriter.Save(documentoFallback);
                        File.WriteAllText(filePath, xaml);

                        App.Logger.LogInfo($"Comprovante salvo em XAML devido a ausencia de impressoras: {filePath}", "PDV");
                        App.Audit.RegistrarAcaoCritica("PDV", "VendaReimpressaFallback", "Venda", venda.Id.ToString(), $"FallbackXaml={filePath}; Total={venda.Total:C}");

                        ExibirMensagem(
                            $"Nenhuma impressora instalada. Comprovante salvo em: {filePath}",
                            "PDV",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        return;
                    }
                    catch (Exception ex)
                    {
                        ExibirMensagem(
                            string.IsNullOrWhiteSpace(snapshot.CaptureError)
                                ? $"Nenhuma impressora instalada foi detectada neste computador.\nFalha ao gerar fallback: {ex.Message}"
                                : $"Nenhuma impressora instalada foi detectada.\n\nDetalhes: {snapshot.CaptureError}\nFalha ao gerar fallback: {ex.Message}",
                            "PDV",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }
                }

                var printDialog = new PrintDialog();
                var impressoraConfigurada = ResolverImpressoraConfigurada(printDialog);

                if (impressoraConfigurada == null && printDialog.ShowDialog() != true)
                {
                    return;
                }

                var documento = _comprovanteService.CriarDocumento(venda);
                var areaLargura = printDialog.PrintableAreaWidth > 0 ? printDialog.PrintableAreaWidth : 302d;
                var areaAltura = printDialog.PrintableAreaHeight > 0 ? printDialog.PrintableAreaHeight : 840d;

                documento.PageWidth = areaLargura;
                documento.PageHeight = areaAltura;
                documento.PagePadding = new Thickness(32);
                documento.ColumnWidth = areaLargura;

                var paginator = ((IDocumentPaginatorSource)documento).DocumentPaginator;
                paginator.PageSize = new Size(areaLargura, areaAltura);

                printDialog.PrintDocument(paginator, $"Comprovante venda {venda.Id}");

                var printerSelecionada = impressoraConfigurada ?? _printerDiagnosticsService.DescribeSelectedPrinter(printDialog.PrintQueue);

                global::PrimoAutoEletrica.App.Logger.LogInfo(
                    $"Reimpressao do PDV enviada para '{printerSelecionada.Name}' (virtual={printerSelecionada.IsVirtual}, driver='{printerSelecionada.DriverName}', porta='{printerSelecionada.PortName}').",
                    "PDV");

                if (printerSelecionada.IsVirtual)
                {
                    global::PrimoAutoEletrica.App.Logger.LogWarning(
                        "A reimpressao do PDV foi enviada para uma impressora virtual. A homologacao final continua dependendo de uma impressora fisica real.",
                        "PDV");
                }

                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "PDV",
                    "VendaReimpressa",
                    "Venda",
                    venda.Id.ToString(),
                    $"FormaPagamento={venda.FormaPagamento}; Total={venda.Total:C}; Impressora={printerSelecionada.Name}; Driver={printerSelecionada.DriverName}; Porta={printerSelecionada.PortName}; Virtual={printerSelecionada.IsVirtual}");
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    $"Nao foi possivel reimprimir a venda: {ex.Message}",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void RegistrarDiagnosticoImpressao()
        {
            if (_diagnosticoImpressaoRegistrado)
            {
                return;
            }

            _diagnosticoImpressaoRegistrado = true;

            var snapshot = _printerDiagnosticsService.CaptureSnapshot();

            global::PrimoAutoEletrica.App.Logger.LogInfo(
                $"Diagnostico de impressao do PDV: {snapshot.BuildSummary()}",
                "PDV");

            if (!snapshot.HasPhysicalPrinter)
            {
                global::PrimoAutoEletrica.App.Logger.LogWarning(
                    "Nenhuma impressora fisica foi detectada para homologacao final do PDV. O ambiente atual depende apenas de fila virtual.",
                    "PDV");
            }
        }

        private PrinterDiagnosticInfo? ResolverImpressoraConfigurada(PrintDialog printDialog)
        {
            var stationConfig = StationService.GetConfiguration(App.RuntimeAppDataPath);

            if (!stationConfig.UseConfiguredPdvPrinter)
            {
                return null;
            }

            if (_printerDiagnosticsService.TryResolvePrinterQueue(
                    stationConfig.PreferredPdvPrinterName,
                    out var printQueue,
                    out var printerInfo,
                    out var message) &&
                printQueue != null &&
                printerInfo != null)
            {
                printDialog.PrintQueue = printQueue;

                App.Logger.LogInfo(
                    $"Reimpressao do PDV usara impressora configurada da estacao: {printerInfo.BuildSummary()}",
                    "PDV");

                return printerInfo;
            }

            App.Logger.LogWarning(
                $"Impressora preferencial do PDV indisponivel; usando seletor manual. {message}",
                "PDV");

            return null;
        }

        private void CancelarUltimaVendaConcluidaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("PDV_CANCELAR_VENDA_REGISTRADA", "Voce nao possui permissao para cancelar uma venda ja concluida."))
            {
                return;
            }

            var venda = SelecionarVendaOperacional(
                titulo: "Cancelar venda concluida",
                subtitulo: "Escolha uma venda recente para executar o estorno completo do PDV.",
                textoConfirmacao: "Cancelar venda",
                incluirCanceladas: true,
                filtro: item => !string.Equals(item.Status, "Cancelada", StringComparison.OrdinalIgnoreCase));

            if (venda == null)
            {
                return;
            }

            var motivoDialog = new OperacaoCaixaWindow(new OperacaoCaixaRequest
            {
                WindowTitle = "Cancelar venda concluida",
                Header = "Cancelamento completo da venda",
                Subheader = $"Venda {venda.Id.ToString()[..8]} | Total {venda.Total:C}\nInforme o motivo do cancelamento para estornar estoque, caixa e financeiro.",
                ObservacoesLabel = "Motivo do cancelamento",
                ConfirmButtonText = "Avancar para cancelamento",
                SolicitarValor = false,
                ObservacoesObrigatorias = true
            });

            ConfigurarOwner(motivoDialog);

            if (motivoDialog.ShowDialog() != true)
            {
                return;
            }

            var confirmado = CriticalActionDialogService.ConfirmarAcao(
                Window.GetWindow(this),
                new CriticalActionRequest
                {
                    WindowTitle = "Cancelar venda concluida",
                    Header = "Estorno completo de venda",
                    Summary = $"Voce esta prestes a cancelar a venda {venda.Id.ToString()[..8]}.",
                    Details = $"Cliente: {venda.Cliente?.Nome ?? "Consumidor final"}\nTotal: {venda.Total:C}\nForma de pagamento: {venda.FormaPagamento}\nMotivo: {motivoDialog.ObservacoesInformadas}",
                    Impact = "O sistema vai reverter estoque, lancamento financeiro e movimento de caixa desta venda.",
                    Keyword = "CANCELAR",
                    ConfirmButtonText = "Cancelar venda concluida"
                });

            if (!confirmado)
            {
                return;
            }

            try
            {
                _vendaService.CancelarVenda(venda.Id, motivoDialog.ObservacoesInformadas);

                if (_viewModel.UltimaVendaFinalizadaId == venda.Id)
                {
                    _viewModel.UltimaVendaFinalizadaId = null;
                }

                CarregarDadosIniciais();
                AtualizarEstadoCaixa();

                ExibirMensagem(
                    "Venda cancelada com estorno completo do fluxo operacional.",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ExibirMensagem(
                    ex.Message,
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            ExibirMensagem(
                mensagem,
                "Acesso negado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
        }

        private void LimparCarrinho()
        {
            _viewModel.Carrinho.Clear();
            _viewModel.ClienteSelecionado = null;
            _viewModel.DescontoGeral = 0;
            _viewModel.FormaPagamentoSelecionada = "Dinheiro";
            _viewModel.PagamentoMistoResumo = string.Empty;

            CarrinhoListView.ItemsSource = null;
            CarrinhoListView.ItemsSource = _viewModel.Carrinho;

            DescontoTextBox.Clear();
            BuscaClienteTextBox.Clear();

            _viewModel.Clientes.Clear();

            foreach (var cliente in _todosClientes.Take(5))
            {
                _viewModel.Clientes.Add(cliente);
            }

            _viewModel.AtualizarTotal();
            AtualizarFormaPagamentoSelecionada();
        }

        private void AtualizarGridCarrinho()
        {
            CarrinhoListView.ItemsSource = null;
            CarrinhoListView.ItemsSource = _viewModel.Carrinho;
            _viewModel.AtualizarTotal();
        }

        private void AtualizarResumoVendasSuspensas()
        {
            if (VendasSuspensas.Count == 0)
            {
                _viewModel.VendasSuspensasResumo = "Nenhuma venda suspensa.";
                return;
            }

            var totalSuspenso = VendasSuspensas.Sum(venda => venda.Total);

            var ultima = VendasSuspensas
                .OrderByDescending(venda => venda.DataSuspensao)
                .First();

            _viewModel.VendasSuspensasResumo =
                $"{VendasSuspensas.Count} venda(s) suspensa(s) somando {totalSuspenso:C}. Ultima: {ultima.DataSuspensao:HH:mm} | {ultima.Total:C}.";
        }

        private void AtualizarFormaPagamentoSelecionada()
        {
            AtualizarEstadoBotaoFormaPagamento(DinheiroButton, "Dinheiro");
            AtualizarEstadoBotaoFormaPagamento(PixButton, "PIX");
            AtualizarEstadoBotaoFormaPagamento(CartaoButton, "Cartao");
            AtualizarEstadoBotaoFormaPagamento(MistoButton, "Misto");
        }

        private void AtualizarEstadoBotaoFormaPagamento(Button? button, string formaPagamento)
        {
            if (button == null)
            {
                return;
            }

            var selecionado = string.Equals(
                _viewModel.FormaPagamentoSelecionada,
                formaPagamento,
                StringComparison.OrdinalIgnoreCase);

            button.Background = selecionado
                ? (Brush)FindResource("PrimaryBrush")
                : (Brush)FindResource("SurfaceAltBrush");

            button.BorderBrush = selecionado
                ? (Brush)FindResource("PrimaryBrush")
                : (Brush)FindResource("BorderBrush");

            button.Foreground = selecionado
                ? (Brush)FindResource("AccentButtonTextBrush")
                : (Brush)FindResource("PrimaryTextBrush");

            button.FontWeight = selecionado
                ? FontWeights.Bold
                : FontWeights.SemiBold;
        }

        private void AtualizarEstadoCaixa()
        {
            try
            {
                var sessao = _caixaService.ObterSessaoAbertaAtual();
                _viewModel.AtualizarCaixa(sessao);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogWarning(
                    $"Falha ao atualizar estado do caixa no PDV: {ex.Message}");
            }
        }

        private CaixaSessaoOperacional? GarantirCaixaAbertoParaVenda()
        {
            var sessao = _caixaService.ObterSessaoAbertaAtual();

            if (sessao != null)
            {
                return sessao;
            }

            ExibirMensagem(
                "Abra o caixa antes de finalizar uma venda. O PDV agora bloqueia pagamentos sem sessao operacional ativa.",
                "Caixa fechado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            AtualizarEstadoCaixa();

            return null;
        }

        private bool GarantirSessaoCaixaAberta()
        {
            if (_caixaService.ObterSessaoAbertaAtual() != null)
            {
                return true;
            }

            ExibirMensagem(
                "Abra o caixa antes de executar esta operacao.",
                "Caixa",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            AtualizarEstadoCaixa();

            return false;
        }

        private void AtualizarProdutosVisiveis(IEnumerable<Produto> produtos)
        {
            _viewModel.Produtos.Clear();

            foreach (var produto in produtos)
            {
                _viewModel.Produtos.Add(produto);
            }
        }

        private int ObterDisponibilidadeOperacional(Produto produto)
        {
            return Math.Max(0, produto.QuantidadeDisponivel);
        }

        private Venda? SelecionarVendaOperacional(
            string titulo,
            string subtitulo,
            string textoConfirmacao,
            bool incluirCanceladas,
            Func<Venda, bool>? filtro = null)
        {
            var sessaoAberta = _caixaService.ObterSessaoAbertaAtual();

            var vendas = _vendaService.ObterHistoricoOperacional(
                limite: 60,
                caixaSessaoId: sessaoAberta?.Id,
                inicio: DateTime.Today.AddDays(-30),
                incluirCanceladas: incluirCanceladas);

            if (vendas.Count == 0 && sessaoAberta?.Id != null)
            {
                vendas = _vendaService.ObterHistoricoOperacional(
                    limite: 60,
                    caixaSessaoId: null,
                    inicio: DateTime.Today.AddDays(-30),
                    incluirCanceladas: incluirCanceladas);
            }

            if (filtro != null)
            {
                vendas = vendas
                    .Where(filtro)
                    .ToList();
            }

            if (vendas.Count == 0)
            {
                ExibirMensagem(
                    "Nenhuma venda recente foi encontrada para esta operacao.",
                    "PDV",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return null;
            }

            var dialog = new SelecionarVendaWindow(
                vendas,
                titulo,
                subtitulo,
                textoConfirmacao);

            ConfigurarOwner(dialog);

            return dialog.ShowDialog() == true
                ? dialog.VendaSelecionada
                : null;
        }

        private static void ConfigurarOwner(Window dialog)
        {
            WindowOwnerHelper.ConfigureOwner(dialog);
        }

        private static ItemVenda CloneItemVenda(ItemVenda item)
        {
            return new ItemVenda
            {
                Produto = item.Produto,
                ProdutoId = item.ProdutoId,
                Tipo = item.Tipo,
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                CustoUnitario = item.CustoUnitario,
                Desconto = item.Desconto
            };
        }

        private sealed record VendaSuspensaPdv(
            Guid Id,
            DateTime DataSuspensao,
            Cliente? Cliente,
            List<ItemVenda> Itens,
            decimal DescontoGeral,
            string FormaPagamento,
            string PagamentoMistoResumo,
            string Operador)
        {
            public decimal Total => Math.Max(0m, Itens.Sum(item => item.Subtotal) - DescontoGeral);
        }

        private MessageBoxResult ExibirMensagem(
            string mensagem,
            string titulo,
            MessageBoxButton botoes,
            MessageBoxImage icone)
        {
            if (global::PrimoAutoEletrica.App.IsAutomatedTestMode)
            {
                var texto = $"{titulo}: {mensagem}";

                if (icone == MessageBoxImage.Error)
                {
                    global::PrimoAutoEletrica.App.Logger.LogError(texto, null, "PDV");
                }
                else if (icone == MessageBoxImage.Warning)
                {
                    global::PrimoAutoEletrica.App.Logger.LogWarning(texto, "PDV");
                }
                else
                {
                    global::PrimoAutoEletrica.App.Logger.LogInfo(texto, "PDV");
                }

                return botoes switch
                {
                    MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel => MessageBoxResult.Yes,
                    MessageBoxButton.OKCancel => MessageBoxResult.OK,
                    _ => MessageBoxResult.OK
                };
            }

            var owner = Window.GetWindow(this);

            return owner != null
                ? MessageBox.Show(owner, mensagem, titulo, botoes, icone)
                : MessageBox.Show(mensagem, titulo, botoes, icone);
        }
    }
}