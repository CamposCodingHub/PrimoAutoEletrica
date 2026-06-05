using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.ViewModels;

namespace PrimoAutoEletrica.Views
{
    public partial class NovoOrcamentoWindow : Window
    {
        private readonly OrcamentosViewModel _viewModel;
        private readonly Dictionary<Guid, Cliente> _clientes;
        private readonly Dictionary<Guid, Produto> _produtos;
        private readonly ObservableCollection<OrcamentoItem> _itensCarrinho;

        public NovoOrcamentoWindow()
        {
            InitializeComponent();

            _viewModel = new OrcamentosViewModel();
            _clientes = App.Repositories.Clientes.ObterTodos().ToDictionary(c => c.Id, c => c);
            _produtos = App.Repositories.Produtos.ObterTodos().ToDictionary(p => p.Id, p => p);
            _itensCarrinho = new ObservableCollection<OrcamentoItem>();

            DataContext = _viewModel;
            _viewModel.NovoOrcamento();
            _viewModel.ItensCarrinho = _itensCarrinho;
            ItensDataGrid.ItemsSource = _itensCarrinho;

            CarregarClientes();
            CarregarProdutos();
            ConfigurarDataPadrao();
            DescontoTextBox.Text = string.Empty;
            AcrescimoTextBox.Text = string.Empty;
            CalcularTotais();
        }

        public NovoOrcamentoWindow(Orcamento orcamentoExistente) : this()
        {
            _viewModel.OrcamentoAtual = orcamentoExistente;
            _itensCarrinho.Clear();

            foreach (var item in orcamentoExistente.Itens)
            {
                _itensCarrinho.Add(item);
            }

            CarregarDadosOrcamento();
            CalcularTotais();
        }

        public NovoOrcamentoWindow(Cliente clientePreSelecionado) : this()
        {
            if (clientePreSelecionado == null || clientePreSelecionado.Id == Guid.Empty)
            {
                return;
            }

            foreach (ComboBoxItem item in ClienteComboBox.Items)
            {
                if (item.Tag is Guid id && id == clientePreSelecionado.Id)
                {
                    ClienteComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void CarregarClientes()
        {
            ClienteComboBox.Items.Clear();

            foreach (var cliente in _clientes.Values.OrderBy(c => c.Nome))
            {
                ClienteComboBox.Items.Add(new ComboBoxItem
                {
                    Content = $"{cliente.Nome} - {cliente.Telefone}",
                    Tag = cliente.Id
                });
            }
        }

        private void CarregarProdutos()
        {
            // Produtos sao carregados sob demanda na janela de selecao.
        }

        private void ConfigurarDataPadrao()
        {
            DataCriacaoDatePicker.SelectedDate = DateTime.Now;
            DataValidadeDatePicker.SelectedDate = DateTime.Now.AddDays(30);
            SelecionarStatus("Em Aberto");
        }

        private void CarregarDadosOrcamento()
        {
            if (_viewModel.OrcamentoAtual == null)
            {
                return;
            }

            if (_viewModel.OrcamentoAtual.ClienteId.HasValue)
            {
                foreach (ComboBoxItem item in ClienteComboBox.Items)
                {
                    if (item.Tag is Guid id && id == _viewModel.OrcamentoAtual.ClienteId.Value)
                    {
                        ClienteComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            SelecionarStatus(_viewModel.OrcamentoAtual.Status);
            DataCriacaoDatePicker.SelectedDate = _viewModel.OrcamentoAtual.DataCriacao;
            DataValidadeDatePicker.SelectedDate = _viewModel.OrcamentoAtual.DataValidade;
            PrazoEntregaTextBox.Text = _viewModel.OrcamentoAtual.PrazoEntrega;
            CondicoesPagamentoTextBox.Text = _viewModel.OrcamentoAtual.CondicoesPagamento;
            ObservacoesTextBox.Text = _viewModel.OrcamentoAtual.Observacoes;
            DescontoTextBox.Text = _viewModel.OrcamentoAtual.Desconto > 0
                ? _viewModel.OrcamentoAtual.Desconto.ToString("F2")
                : string.Empty;
            AcrescimoTextBox.Text = _viewModel.OrcamentoAtual.Acrescimo > 0
                ? _viewModel.OrcamentoAtual.Acrescimo.ToString("F2")
                : string.Empty;
        }

        private void SelecionarStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                StatusComboBox.SelectedIndex = 0;
                return;
            }

            foreach (ComboBoxItem item in StatusComboBox.Items)
            {
                var conteudo = item.Content?.ToString();
                if (string.Equals(conteudo, status, StringComparison.OrdinalIgnoreCase))
                {
                    StatusComboBox.SelectedItem = item;
                    return;
                }
            }

            StatusComboBox.SelectedIndex = 0;
        }

        private Guid? ObterClienteSelecionadoId()
        {
            return ClienteComboBox.SelectedItem is ComboBoxItem { Tag: Guid clienteId }
                ? clienteId
                : null;
        }

        private string ObterStatusSelecionadoOuPadrao(string statusPadrao)
        {
            if (StatusComboBox.SelectedItem is ComboBoxItem item &&
                !string.IsNullOrWhiteSpace(item.Content?.ToString()))
            {
                return item.Content.ToString()!.Trim();
            }

            return statusPadrao;
        }

        private void RecalcularItem(OrcamentoItem item)
        {
            var valorBruto = item.Quantidade * item.PrecoUnitario;
            item.Subtotal = Math.Max(0m, valorBruto - item.Desconto);

            var custoTotal = item.Quantidade * item.PrecoCusto;
            item.LucroEstimado = item.Subtotal - custoTotal;
            item.MargemLucro = item.Subtotal > 0
                ? (item.LucroEstimado / item.Subtotal) * 100m
                : 0m;
        }

        private void RecalcularTodosOsItens()
        {
            foreach (var item in _itensCarrinho)
            {
                RecalcularItem(item);
            }
        }

        private void AdicionarProduto_Click(object sender, RoutedEventArgs e)
        {
            var selecionarProdutoWindow = new SelecionarProdutoWindow(_produtos.Values.OrderBy(p => p.Nome).ToList())
            {
                Owner = this
            };

            if (selecionarProdutoWindow.ShowDialog() == true && selecionarProdutoWindow.ProdutoSelecionado != null)
            {
                var quantidadeWindow = new QuantidadeProdutoWindow(selecionarProdutoWindow.ProdutoSelecionado)
                {
                    Owner = this
                };

                if (quantidadeWindow.ShowDialog() == true)
                {
                    var produto = selecionarProdutoWindow.ProdutoSelecionado;
                    var quantidade = quantidadeWindow.Quantidade;

                    var itemExistente = _itensCarrinho.FirstOrDefault(i => i.UsaEstoque && i.ProdutoId == produto.Id);
                    if (itemExistente != null)
                    {
                        itemExistente.Quantidade += quantidade;
                        RecalcularItem(itemExistente);
                    }
                    else
                    {
                        var novoItem = new OrcamentoItem
                        {
                            Id = Guid.NewGuid(),
                            OrcamentoId = _viewModel.OrcamentoAtual?.Id ?? Guid.NewGuid(),
                            ProdutoId = produto.Id,
                            Tipo = "Produto",
                            ProdutoNome = produto.Nome,
                            ProdutoCodigo = produto.Codigo,
                            ProdutoCategoria = produto.Categoria,
                            ProdutoMarca = produto.Marca,
                            ProdutoAplicacao = string.Empty,
                            Quantidade = quantidade,
                            PrecoUnitario = produto.PrecoVenda,
                            PrecoCusto = produto.PrecoCompra,
                            Desconto = 0,
                            EstoqueDisponivel = produto.QuantidadeEstoque
                        };

                        RecalcularItem(novoItem);
                        _itensCarrinho.Add(novoItem);
                    }

                    ItensDataGrid.Items.Refresh();
                    CalcularTotais();
                }
            }
        }

        private void AdicionarMaoDeObra_Click(object sender, RoutedEventArgs e)
        {
            _itensCarrinho.Add(new OrcamentoItem
            {
                Id = Guid.NewGuid(),
                OrcamentoId = _viewModel.OrcamentoAtual?.Id ?? Guid.NewGuid(),
                Tipo = "Servico",
                ProdutoNome = "Mao de obra tecnica",
                ProdutoCodigo = "SERVICO",
                ProdutoCategoria = "Mao de obra",
                ProdutoMarca = "Oficina",
                ProdutoAplicacao = string.Empty,
                Quantidade = 1,
                PrecoUnitario = 0m,
                PrecoCusto = 0m,
                Desconto = 0m,
                EstoqueDisponivel = 0,
                Observacoes = "Servico adicionado manualmente ao orcamento."
            });

            ItensDataGrid.Items.Refresh();
            CalcularTotais();
        }

        private void RemoverItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OrcamentoItem item)
            {
                _itensCarrinho.Remove(item);
                ItensDataGrid.Items.Refresh();
                CalcularTotais();
            }
        }

        private void ItensDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(CalcularTotais, DispatcherPriority.Background);
        }

        private void CalcularTotais()
        {
            RecalcularTodosOsItens();

            var subtotal = _itensCarrinho.Sum(i => i.Subtotal);
            var desconto = TryObterDecimalOpcional(DescontoTextBox.Text, out var descontoGeral) ? descontoGeral : 0m;
            var acrescimo = TryObterDecimalOpcional(AcrescimoTextBox.Text, out var acrescimoGeral) ? acrescimoGeral : 0m;
            var total = Math.Max(0m, subtotal - desconto + acrescimo);
            var lucroEstimado = _itensCarrinho.Sum(i => i.LucroEstimado);
            var margemLucro = total > 0 ? (lucroEstimado / total) * 100m : 0m;
            var comissao = total * 0.05m;

            SubtotalTextBlock.Text = subtotal.ToString("C");
            TotalTextBlock.Text = total.ToString("C");
            MargemLucroTextBlock.Text = margemLucro.ToString("F2") + "%";
            LucroEstimadoTextBlock.Text = lucroEstimado.ToString("C");
            ComissaoTextBlock.Text = comissao.ToString("C");

            _viewModel.Subtotal = subtotal;
            _viewModel.Desconto = desconto;
            _viewModel.Acrescimo = acrescimo;
            _viewModel.Total = total;
            _viewModel.LucroEstimado = lucroEstimado;
            _viewModel.MargemLucro = margemLucro;
        }

        private void Desconto_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularTotais();
        }

        private void Acrescimo_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularTotais();
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            if (ValidarCampos())
            {
                SalvarOrcamento(ObterStatusSelecionadoOuPadrao("Aprovado"));
                DialogResult = true;
                Close();
            }
        }

        private void SalvarRascunho_Click(object sender, RoutedEventArgs e)
        {
            if (ValidarCampos())
            {
                SelecionarStatus("Em Aberto");
                SalvarOrcamento("Em Aberto");
                DialogResult = true;
                Close();
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidarCampos()
        {
            if (ClienteComboBox.SelectedItem == null)
            {
                MessageBox.Show("Selecione um cliente.", "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (_itensCarrinho.Count == 0)
            {
                MessageBox.Show("Adicione pelo menos um item ao orcamento.", "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!TryObterDecimalOpcional(DescontoTextBox.Text, out var desconto) || desconto < 0)
            {
                MessageBox.Show("Informe um desconto valido.", "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                DescontoTextBox.Focus();
                return false;
            }

            if (!TryObterDecimalOpcional(AcrescimoTextBox.Text, out var acrescimo) || acrescimo < 0)
            {
                MessageBox.Show("Informe um acrescimo valido.", "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                AcrescimoTextBox.Focus();
                return false;
            }

            var erroData = CadastroValidationHelper.ValidarIntervaloDatas(
                DataCriacaoDatePicker.SelectedDate,
                DataValidadeDatePicker.SelectedDate,
                "a data de criacao",
                "a data de validade",
                obrigatorioInicial: true,
                obrigatorioFinal: false);
            if (!string.IsNullOrWhiteSpace(erroData))
            {
                MessageBox.Show(erroData, "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                DataValidadeDatePicker.Focus();
                return false;
            }

            return true;
        }

        private void SalvarOrcamento(string status)
        {
            if (_viewModel.OrcamentoAtual == null)
            {
                return;
            }

            CalcularTotais();

            var clienteId = ObterClienteSelecionadoId();
            _viewModel.OrcamentoAtual.ClienteId = clienteId;
            _viewModel.OrcamentoAtual.Cliente = clienteId.HasValue && _clientes.TryGetValue(clienteId.Value, out var cliente)
                ? cliente
                : null;
            _viewModel.OrcamentoAtual.Status = status;
            _viewModel.OrcamentoAtual.DataCriacao = DataCriacaoDatePicker.SelectedDate ?? DateTime.Now;
            _viewModel.OrcamentoAtual.DataValidade = DataValidadeDatePicker.SelectedDate;
            _viewModel.OrcamentoAtual.PrazoEntrega = PrazoEntregaTextBox.Text?.Trim() ?? string.Empty;
            _viewModel.OrcamentoAtual.CondicoesPagamento = CondicoesPagamentoTextBox.Text?.Trim() ?? string.Empty;
            _viewModel.OrcamentoAtual.Observacoes = ObservacoesTextBox.Text?.Trim() ?? string.Empty;
            _viewModel.OrcamentoAtual.Itens = _itensCarrinho.ToList();
            _viewModel.OrcamentoAtual.Subtotal = _viewModel.Subtotal;
            _viewModel.OrcamentoAtual.Desconto = _viewModel.Desconto;
            _viewModel.OrcamentoAtual.Acrescimo = _viewModel.Acrescimo;
            _viewModel.OrcamentoAtual.Total = _viewModel.Total;
            _viewModel.OrcamentoAtual.MargemLucro = _viewModel.MargemLucro;
            _viewModel.OrcamentoAtual.LucroEstimado = _viewModel.LucroEstimado;
            _viewModel.OrcamentoAtual.ComissaoVendedor = _viewModel.Total * 0.05m;
            _viewModel.OrcamentoAtual.ImpostosEstimados = _viewModel.Total * 0.18m;

            _viewModel.SalvarOrcamento();
        }

        private static bool TryObterDecimalOpcional(string? valorTexto, out decimal valor)
        {
            if (string.IsNullOrWhiteSpace(valorTexto))
            {
                valor = 0m;
                return true;
            }

            return decimal.TryParse(valorTexto.Trim(), out valor);
        }
    }

    // Janela auxiliar para selecionar produto.
    public class SelecionarProdutoWindow : Window
    {
        public Produto? ProdutoSelecionado { get; private set; }

        public SelecionarProdutoWindow(List<Produto> produtos)
        {
            var appBackgroundBrush = Application.Current.TryFindResource("AppBackgroundBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(246, 248, 251));
            var cardBackgroundBrush = Application.Current.TryFindResource("CardBackgroundBrush") as Brush
                ?? Brushes.White;
            var borderBrush = Application.Current.TryFindResource("BorderBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(226, 232, 240));
            var primaryTextBrush = Application.Current.TryFindResource("PrimaryTextBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(15, 23, 42));
            var secondaryTextBrush = Application.Current.TryFindResource("SecondaryTextBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(100, 116, 139));

            Title = "Selecionar Produto";
            Width = 760;
            Height = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;
            Background = appBackgroundBrush;

            var container = new Border
            {
                Margin = new Thickness(16),
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(18),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                Background = cardBackgroundBrush
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new Border
            {
                Padding = new Thickness(24),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            header.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "Selecionar produto",
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = primaryTextBrush
                    },
                    new TextBlock
                    {
                        Text = "Escolha um item ativo do estoque para adicionar ao orcamento.",
                        Margin = new Thickness(0, 8, 0, 0),
                        FontSize = 13,
                        Foreground = secondaryTextBrush
                    }
                }
            };

            var dataGrid = new DataGrid
            {
                Style = Application.Current.TryFindResource("PremiumDataGrid") as Style,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                ItemsSource = produtos,
                Margin = new Thickness(24),
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                FontSize = 13
            };

            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Codigo", Binding = new System.Windows.Data.Binding("Codigo"), Width = 110 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Nome", Binding = new System.Windows.Data.Binding("Nome"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Categoria", Binding = new System.Windows.Data.Binding("Categoria"), Width = 120 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Preco", Binding = new System.Windows.Data.Binding("PrecoVenda") { StringFormat = "C" }, Width = 110 });
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "Estoque", Binding = new System.Windows.Data.Binding("QuantidadeEstoque"), Width = 90 });

            dataGrid.MouseDoubleClick += (_, _) =>
            {
                if (dataGrid.SelectedItem is Produto produto)
                {
                    ProdutoSelecionado = produto;
                    DialogResult = true;
                    Close();
                }
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(24)
            };

            var adicionarButton = new Button
            {
                Content = "Adicionar",
                Width = 120,
                Margin = new Thickness(0, 0, 10, 0),
                Style = Application.Current.TryFindResource("ModalAccentButton") as Style
            };
            adicionarButton.Click += (_, _) =>
            {
                if (dataGrid.SelectedItem is Produto produto)
                {
                    ProdutoSelecionado = produto;
                    DialogResult = true;
                    Close();
                }
            };

            var cancelarButton = new Button
            {
                Content = "Cancelar",
                Width = 110,
                Style = Application.Current.TryFindResource("ModalSecondaryButton") as Style
            };
            cancelarButton.Click += (_, _) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(adicionarButton);
            buttonPanel.Children.Add(cancelarButton);

            grid.Children.Add(header);
            Grid.SetRow(header, 0);

            grid.Children.Add(dataGrid);
            Grid.SetRow(dataGrid, 1);

            grid.Children.Add(buttonPanel);
            Grid.SetRow(buttonPanel, 2);

            container.Child = grid;
            Content = container;
        }
    }

    // Janela auxiliar para informar quantidade.
    public class QuantidadeProdutoWindow : Window
    {
        public int Quantidade { get; private set; } = 1;

        public QuantidadeProdutoWindow(Produto produto, int quantidadeInicial = 1)
        {
            var appBackgroundBrush = Application.Current.TryFindResource("AppBackgroundBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(246, 248, 251));
            var cardBackgroundBrush = Application.Current.TryFindResource("CardBackgroundBrush") as Brush
                ?? Brushes.White;
            var borderBrush = Application.Current.TryFindResource("BorderBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(226, 232, 240));
            var primaryTextBrush = Application.Current.TryFindResource("PrimaryTextBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(15, 23, 42));
            var secondaryTextBrush = Application.Current.TryFindResource("SecondaryTextBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(100, 116, 139));
            var warningCardBackgroundBrush = Application.Current.TryFindResource("WarningCardBackgroundBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(255, 247, 237));
            var warningBrush = Application.Current.TryFindResource("WarningBrush") as Brush
                ?? new SolidColorBrush(Color.FromRgb(154, 52, 18));

            Quantidade = Math.Max(1, quantidadeInicial);
            Title = "Informar Quantidade";
            Width = 460;
            MinHeight = 340;
            MaxHeight = Math.Max(360, SystemParameters.WorkArea.Height - 48);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            SizeToContent = SizeToContent.Height;
            Background = appBackgroundBrush;

            var container = new Border
            {
                Margin = new Thickness(16),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Background = cardBackgroundBrush
            };

            var grid = new Grid();
            for (var indice = 0; indice < 6; indice++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            var header = new Border
            {
                Padding = new Thickness(24),
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };

            header.Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "Quantidade do item",
                        FontSize = 22,
                        FontWeight = FontWeights.Bold,
                        Foreground = primaryTextBrush
                    },
                    new TextBlock
                    {
                        Text = "Defina a quantidade para o item selecionado.",
                        Margin = new Thickness(0, 8, 0, 0),
                        FontSize = 13,
                        Foreground = secondaryTextBrush
                    }
                }
            };

            var produtoInfo = new TextBlock
            {
                Text = $"{produto.Nome} - {produto.Codigo}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(24, 24, 24, 8),
                Foreground = primaryTextBrush
            };

            var precoInfo = new TextBlock
            {
                Text = $"Preco: {produto.PrecoVenda:C} | Estoque: {produto.QuantidadeEstoque}",
                FontSize = 14,
                Margin = new Thickness(24, 0, 24, 16),
                Foreground = secondaryTextBrush
            };

            var quantidadePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(24, 0, 24, 16)
            };

            var label = new TextBlock
            {
                Text = "Quantidade:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.SemiBold,
                Foreground = primaryTextBrush
            };

            var textBox = new TextBox
            {
                Width = 120,
                Text = Quantidade.ToString(),
                Style = Application.Current.TryFindResource("PremiumTextBox") as Style
            };
            textBox.TextChanged += (_, _) =>
            {
                if (int.TryParse(textBox.Text, out var qtd) && qtd > 0)
                {
                    Quantidade = qtd;
                }
            };

            quantidadePanel.Children.Add(label);
            quantidadePanel.Children.Add(textBox);

            var alertaInfo = new Border
            {
                Margin = new Thickness(24, 0, 24, 0),
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(12),
                Background = warningCardBackgroundBrush,
                BorderBrush = warningBrush,
                BorderThickness = new Thickness(1),
                Child = new TextBlock
                {
                    Text = "A quantidade pode ser ajustada depois no carrinho, se necessario.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = warningBrush
                }
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(24)
            };

            var confirmarButton = new Button
            {
                Content = "Confirmar",
                Width = 120,
                Margin = new Thickness(0, 0, 10, 0),
                Style = Application.Current.TryFindResource("ModalAccentButton") as Style
            };
            confirmarButton.Click += (_, _) =>
            {
                if (Quantidade <= 0)
                {
                    MessageBox.Show("Informe uma quantidade valida.", "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
                Close();
            };

            var cancelarButton = new Button
            {
                Content = "Cancelar",
                Width = 110,
                Style = Application.Current.TryFindResource("ModalSecondaryButton") as Style
            };
            cancelarButton.Click += (_, _) =>
            {
                DialogResult = false;
                Close();
            };

            buttonPanel.Children.Add(confirmarButton);
            buttonPanel.Children.Add(cancelarButton);

            grid.Children.Add(header);
            Grid.SetRow(header, 0);

            grid.Children.Add(produtoInfo);
            Grid.SetRow(produtoInfo, 1);

            grid.Children.Add(precoInfo);
            Grid.SetRow(precoInfo, 2);

            grid.Children.Add(quantidadePanel);
            Grid.SetRow(quantidadePanel, 3);

            grid.Children.Add(alertaInfo);
            Grid.SetRow(alertaInfo, 4);

            grid.Children.Add(buttonPanel);
            Grid.SetRow(buttonPanel, 5);

            container.Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = grid
            };
            Content = container;
        }
    }
}
