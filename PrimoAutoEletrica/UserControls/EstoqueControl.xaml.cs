using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.UserControls
{
    public partial class EstoqueControl : UserControl
    {
        private readonly EstoqueViewModel _viewModel;
        private readonly PermissionService _permissionService;

        public EstoqueControl()
        {
            InitializeComponent();

            _viewModel = App.Services.GetRequiredService<EstoqueViewModel>();
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);

            DataContext = _viewModel;
            Focusable = true;
            PreviewKeyDown += EstoqueControl_PreviewKeyDown;
            Loaded += EstoqueControl_Loaded;
        }

        private void EstoqueControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                NovoProduto_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                BuscaProdutoTextBox.Focus();
                BuscaProdutoTextBox.SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter
                && Keyboard.Modifiers == ModifierKeys.None
                && ProdutosDataGrid.IsKeyboardFocusWithin
                && ObterProdutoSelecionado(null) != null)
            {
                EditarProduto_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void EstoqueControl_Loaded(object sender, RoutedEventArgs e)
        {
            ProdutosDataGrid.ItemsSource = _viewModel.ProdutosVisiveis;
            _viewModel.CarregarProdutos();
            Dispatcher.BeginInvoke(new Action(() => BuscaProdutoTextBox.Focus()), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                _viewModel.TextoBusca = tb.Text;
            _viewModel.FiltrarProdutos();
        }

        private void NovoProduto_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissionService.TemPermissaoCodigo("ESTOQUE_CRIAR"))
            {
                MessageBox.Show("Você não tem permissão para criar produtos.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var novaProdutoWindow = new NovoProdutoWindow { Owner = Window.GetWindow(this) };
            if (novaProdutoWindow.ShowDialog() == true)
                _viewModel.CarregarProdutos();
        }

        private void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissionService.TemPermissaoCodigo("ESTOQUE_EDITAR"))
            {
                MessageBox.Show("Você não tem permissão para editar produtos.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var produto = ObterProdutoSelecionado(sender);
            if (produto == null)
            {
                MessageBox.Show("Selecione um produto para editar.", "Estoque", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var completo = _viewModel.ObterProdutoPorId(produto.Id) ?? produto;
            var window = new EditarProdutoWindow(completo) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true)
                _viewModel.CarregarProdutos();
        }

        private void AjustarEstoque_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissionService.TemPermissaoCodigo("ESTOQUE_AJUSTAR"))
            {
                MessageBox.Show("Você não tem permissão para ajustar estoque.", "Acesso Negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new AjusteEstoqueWindow { Owner = Window.GetWindow(this) };
            window.ShowDialog();
            _viewModel.CarregarProdutos();
        }

        private void ImprimirEtiqueta_Click(object sender, RoutedEventArgs e)
        {
            var produto = ObterProdutoSelecionado(sender);
            if (produto == null)
            {
                MessageBox.Show("Selecione um produto para imprimir a etiqueta.", "Estoque", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var completo = _viewModel.ObterProdutoPorId(produto.Id) ?? produto;
                _viewModel.AbrirEtiqueta(completo);
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Falha ao gerar etiqueta de produto.", ex);
                MessageBox.Show($"Nao foi possivel gerar a etiqueta.\n{ex.Message}", "Estoque", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HistoricoEstoque_Click(object sender, RoutedEventArgs e)
        {
            var produto = ObterProdutoSelecionado(sender);
            if (produto == null)
            {
                MessageBox.Show("Selecione um produto para ver o historico.", "Estoque", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var completo = _viewModel.ObterProdutoPorId(produto.Id) ?? produto;
                var historico = new EstoqueOperationalService(App.Database, App.Logger)
                    .ObterHistoricoProduto(completo.Id);
                var window = new HistoricoEstoqueWindow(completo, historico)
                {
                    Owner = Window.GetWindow(this)
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Falha ao abrir historico de estoque.", ex);
                MessageBox.Show($"Nao foi possivel abrir o historico.\n{ex.Message}", "Estoque", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PaginaAnterior_Click(object sender, RoutedEventArgs e) => _viewModel.PreviousPage();

        private void PaginaProxima_Click(object sender, RoutedEventArgs e) => _viewModel.NextPage();

        private Produto? ObterProdutoSelecionado(object? sender)
        {
            if (sender is FrameworkElement { Tag: Produto tagged })
                return tagged;
            if (sender is FrameworkElement { DataContext: Produto fromContext })
                return fromContext;
            return ProdutosDataGrid.SelectedItem as Produto;
        }
    }
}
