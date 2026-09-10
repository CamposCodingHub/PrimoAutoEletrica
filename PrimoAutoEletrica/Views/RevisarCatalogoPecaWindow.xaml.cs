using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Catalogo;
using System;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public partial class RevisarCatalogoPecaWindow : Window
    {
        private readonly CatalogoPecasService _catalogoPecasService;
        private readonly CatalogoParaProdutoService _catalogoParaProdutoService;
        private readonly PermissionService _permissionService;
        private readonly bool _somenteLeitura;
        private readonly CatalogoPeca _item;

        public bool ProdutoCriadoNoFluxo { get; private set; }

        public RevisarCatalogoPecaWindow(CatalogoPeca item, bool somenteLeitura = false)
        {
            InitializeComponent();
            _item = item ?? throw new ArgumentNullException(nameof(item));
            _somenteLeitura = somenteLeitura;
            _catalogoPecasService = new CatalogoPecasService();
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);
            _catalogoParaProdutoService = new CatalogoParaProdutoService(_catalogoPecasService, _permissionService);

            ConfigurarStatus();
            PreencherCampos();
            AplicarModoSomenteLeitura();
        }

        private void ConfigurarStatus()
        {
            StatusComboBox.ItemsSource = new[]
            {
                "Pendente",
                "Revisado",
                "Incompleto",
                "DuplicadoProvavel",
                "ConvertidoEstoque",
                "Ignorado"
            };
        }

        private void PreencherCampos()
        {
            CodigoFabricanteTextBox.Text = _item.CodigoFabricante;
            NomeTextBox.Text = _item.Nome;
            DescricaoTextBox.Text = _item.Descricao;
            MarcaTextBox.Text = _item.Marca;
            CategoriaTextBox.Text = _item.Categoria;
            SubcategoriaTextBox.Text = _item.Subcategoria;
            AplicacaoTextBox.Text = _item.Aplicacao;
            VoltagemTextBox.Text = _item.Voltagem;
            AmperagemTextBox.Text = _item.Amperagem;
            TerminaisTextBox.Text = _item.QuantidadeTerminais;
            PaginaCatalogoTextBox.Text = _item.PaginaCatalogo;
            ObservacoesTextBox.Text = _item.ObservacoesTecnicas;
            StatusComboBox.SelectedItem = string.IsNullOrWhiteSpace(_item.StatusRevisao) ? "Pendente" : _item.StatusRevisao;
            OrigemItemText.Text =
                $"Fonte: {_item.FonteCatalogo}\n" +
                $"Arquivo: {_item.ArquivoOrigem}\n" +
                $"Codigo normalizado: {_item.CodigoNormalizado}\n" +
                $"Produto estoque vinculado: {(_item.ProdutoEstoqueId.HasValue ? _item.ProdutoEstoqueId.ToString() : "Nao")}";
        }

        private void AplicarModoSomenteLeitura()
        {
            if (!_somenteLeitura)
            {
                return;
            }

            HeaderTitleText.Text = "Visualizar Item do Catalogo";
            SalvarButton.Visibility = Visibility.Collapsed;

            foreach (var control in FindVisualChildren<Control>(this))
            {
                if (control == CriarProdutoButton)
                {
                    continue;
                }

                if (control is TextBox textBox)
                {
                    textBox.IsReadOnly = true;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.IsEnabled = false;
                }
            }
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SalvarAlteracoes(exibirMensagem: true);
                WindowInteractionHelper.CloseWithDialogResult(this, true, "Catalogo");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Falha ao salvar a revisao:\n{ex.Message}",
                    "Catalogo",
                    MessageBoxImage.Error,
                    "Catalogo",
                    ex);
            }
        }

        private void CriarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_somenteLeitura)
                {
                    SalvarAlteracoes(exibirMensagem: false);
                }

                var produto = _catalogoParaProdutoService.IniciarConversao(this, _item);
                if (produto == null)
                {
                    return;
                }

                ProdutoCriadoNoFluxo = true;
                WindowInteractionHelper.CloseWithDialogResult(this, true, "Catalogo");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Falha ao criar o produto a partir do catalogo:\n{ex.Message}",
                    "Catalogo",
                    MessageBoxImage.Error,
                    "Catalogo",
                    ex);
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            WindowInteractionHelper.CloseWithDialogResult(this, false, "Catalogo");
        }

        private void SalvarAlteracoes(bool exibirMensagem)
        {
            if (!_permissionService.TemPermissaoCodigo("CATALOGO_REVISAR"))
            {
                throw new InvalidOperationException("Sua sessao nao possui permissao para revisar itens do catalogo.");
            }

            _item.CodigoFabricante = CodigoFabricanteTextBox.Text.Trim();
            _item.Nome = NomeTextBox.Text.Trim();
            _item.Descricao = DescricaoTextBox.Text.Trim();
            _item.Marca = MarcaTextBox.Text.Trim();
            _item.Categoria = CategoriaTextBox.Text.Trim();
            _item.Subcategoria = SubcategoriaTextBox.Text.Trim();
            _item.Aplicacao = AplicacaoTextBox.Text.Trim();
            _item.Voltagem = VoltagemTextBox.Text.Trim();
            _item.Amperagem = AmperagemTextBox.Text.Trim();
            _item.QuantidadeTerminais = TerminaisTextBox.Text.Trim();
            _item.PaginaCatalogo = PaginaCatalogoTextBox.Text.Trim();
            _item.ObservacoesTecnicas = ObservacoesTextBox.Text.Trim();
            _item.StatusRevisao = StatusComboBox.SelectedItem?.ToString() ?? "Pendente";
            _item.DataAtualizacao = DateTime.Now;

            _catalogoPecasService.Atualizar(_item);

            if (exibirMensagem)
            {
                WindowInteractionHelper.ShowMessage(
                    "Revisao salva com sucesso.",
                    "Catalogo",
                    MessageBoxImage.Information,
                    "Catalogo");
            }
        }

        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            if (dependencyObject == null)
            {
                yield break;
            }

            for (var index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(dependencyObject); index++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(dependencyObject, index);
                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
