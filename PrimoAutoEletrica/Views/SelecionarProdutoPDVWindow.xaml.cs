using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.Views
{
    public partial class SelecionarProdutoPDVWindow : Window
    {
        private readonly List<Produto> _todosProdutos;
        private List<Produto> _produtosFiltrados = new();

        public Produto? ProdutoSelecionado { get; private set; }

        public bool FecharAposAdicionar { get; private set; }

        public SelecionarProdutoPDVWindow(IEnumerable<Produto> produtos)
        {
            InitializeComponent();

            _todosProdutos = produtos?
                .Where(produto => produto != null)
                .OrderBy(produto => produto.Nome)
                .ToList() ?? new List<Produto>();

            Loaded += SelecionarProdutoPDVWindow_Loaded;
        }

        private void SelecionarProdutoPDVWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigurarFiltros();
            AplicarFiltros();

            BuscaTextBox.Focus();
            BuscaTextBox.SelectAll();
        }

        private void ConfigurarFiltros()
        {
            var categorias = new List<string> { "Todas" };

            categorias.AddRange(
                _todosProdutos
                    .Select(produto => produto.Categoria)
                    .Where(categoria => !string.IsNullOrWhiteSpace(categoria))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(categoria => categoria));

            CategoriaComboBox.ItemsSource = categorias;
            CategoriaComboBox.SelectedItem = "Todas";

            StatusComboBox.ItemsSource = new List<string>
            {
                "Todos",
                "Somente disponíveis",
                "Sem estoque",
                "Sem preço",
                "Sem código de barras",
                "Sem SKU"
            };

            StatusComboBox.SelectedItem = "Somente disponíveis";
        }

        private void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void BuscaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SelecionarProduto(fecharAposAdicionar: true);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
                e.Handled = true;
            }
        }

        private void CategoriaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void LimparFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            BuscaTextBox.Clear();
            CategoriaComboBox.SelectedItem = "Todas";
            StatusComboBox.SelectedItem = "Somente disponíveis";
            BuscaTextBox.Focus();
        }

        private void ProdutosDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelecionarProduto(fecharAposAdicionar: true);
        }

        private void AdicionarLinhaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Produto produto })
            {
                ProdutosDataGrid.SelectedItem = produto;
            }

            SelecionarProduto(fecharAposAdicionar: true);
        }

        private void AdicionarContinuarButton_Click(object sender, RoutedEventArgs e)
        {
            SelecionarProduto(fecharAposAdicionar: false);
        }

        private void AdicionarFecharButton_Click(object sender, RoutedEventArgs e)
        {
            SelecionarProduto(fecharAposAdicionar: true);
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SelecionarProduto(bool fecharAposAdicionar)
        {
            if (ProdutosDataGrid.SelectedItem is not Produto produto)
            {
                MessageBox.Show(
                    this,
                    "Selecione um produto para adicionar ao carrinho.",
                    "Selecionar produto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (produto.QuantidadeDisponivel <= 0)
            {
                MessageBox.Show(
                    this,
                    "Este produto não possui disponibilidade operacional no estoque.",
                    "Produto sem estoque",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (produto.PrecoVenda <= 0)
            {
                var confirmar = MessageBox.Show(
                    this,
                    "Este produto está sem preço de venda cadastrado.\n\nDeseja adicionar mesmo assim?",
                    "Produto sem preço",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmar != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            ProdutoSelecionado = produto;
            FecharAposAdicionar = fecharAposAdicionar;
            DialogResult = true;
            Close();
        }

        private void AplicarFiltros()
        {
            if (ProdutosDataGrid == null)
            {
                return;
            }

            IEnumerable<Produto> consulta = _todosProdutos;

            var termo = BuscaTextBox?.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(termo))
            {
                consulta = consulta.Where(produto =>
                    Contem(produto.Nome, termo)
                    || Contem(produto.Codigo, termo)
                    || Contem(produto.SKU, termo)
                    || Contem(produto.CodigoBarras, termo)
                    || Contem(produto.Categoria, termo)
                    || Contem(produto.Marca, termo)
                    || Contem(produto.Fornecedor, termo));
            }

            var categoria = CategoriaComboBox?.SelectedItem as string;

            if (!string.IsNullOrWhiteSpace(categoria) &&
                !string.Equals(categoria, "Todas", StringComparison.OrdinalIgnoreCase))
            {
                consulta = consulta.Where(produto =>
                    string.Equals(produto.Categoria, categoria, StringComparison.OrdinalIgnoreCase));
            }

            var status = StatusComboBox?.SelectedItem as string;

            consulta = status switch
            {
                "Somente disponíveis" => consulta.Where(produto => produto.QuantidadeDisponivel > 0),
                "Sem estoque" => consulta.Where(produto => produto.QuantidadeDisponivel <= 0),
                "Sem preço" => consulta.Where(produto => produto.PrecoVenda <= 0),
                "Sem código de barras" => consulta.Where(produto => string.IsNullOrWhiteSpace(produto.CodigoBarras)),
                "Sem SKU" => consulta.Where(produto => string.IsNullOrWhiteSpace(produto.SKU)),
                _ => consulta
            };

            _produtosFiltrados = consulta
                .OrderBy(produto => produto.Nome)
                .Take(500)
                .ToList();

            ProdutosDataGrid.ItemsSource = _produtosFiltrados;

            ResumoTextBlock.Text = $"Produtos encontrados: {_produtosFiltrados.Count}";
        }

        private static bool Contem(string? origem, string termo)
        {
            return !string.IsNullOrWhiteSpace(origem)
                && origem.Contains(termo, StringComparison.OrdinalIgnoreCase);
        }
    }
}