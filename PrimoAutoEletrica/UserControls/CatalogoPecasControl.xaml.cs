using Microsoft.Win32;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Catalogo;
using PrimoAutoEletrica.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.UserControls
{
    public partial class CatalogoPecasControl : UserControl
    {
        private readonly CatalogoPecasService _catalogoPecasService = null!;
        private readonly CatalogoImportacaoService _catalogoImportacaoService = null!;
        private readonly CatalogoParaProdutoService _catalogoParaProdutoService = null!;
        private readonly PermissionService _permissionService = null!;
        private readonly List<CatalogoPeca> _todosItens = new();
        private readonly List<CatalogoPeca> _itensFiltrados = new();
        private readonly List<CatalogoImportacao> _historicoImportacoes = new();
        private bool _loadedOnce;
        private bool _suspendFilters;

        public CatalogoPecasControl()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            _catalogoPecasService = new CatalogoPecasService();
            _catalogoImportacaoService = new CatalogoImportacaoService(_catalogoPecasService);
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger);
            _catalogoParaProdutoService = new CatalogoParaProdutoService(_catalogoPecasService, _permissionService);

            InicializarFiltros();
            Loaded += CatalogoPecasControl_Loaded;
        }

        private void CatalogoPecasControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_loadedOnce)
            {
                return;
            }

            _loadedOnce = true;
            CarregarDados();
        }

        private void InicializarFiltros()
        {
            _suspendFilters = true;
            MarcaComboBox.ItemsSource = new[] { "Todas as marcas" };
            MarcaComboBox.SelectedIndex = 0;
            CategoriaComboBox.ItemsSource = new[] { "Todas as categorias" };
            CategoriaComboBox.SelectedIndex = 0;
            StatusComboBox.ItemsSource = new[]
            {
                "Todos os status",
                "Pendente",
                "Revisado",
                "Incompleto",
                "DuplicadoProvavel",
                "ConvertidoEstoque",
                "Ignorado"
            };
            StatusComboBox.SelectedIndex = 0;
            SomentePendentesCheckBox.IsChecked = false;
            _suspendFilters = false;
        }

        private void CarregarDados()
        {
            try
            {
                _todosItens.Clear();
                _todosItens.AddRange(_catalogoPecasService.ObterTodos());

                _historicoImportacoes.Clear();
                _historicoImportacoes.AddRange(_catalogoImportacaoService.ObterHistoricoImportacoes(25));

                AtualizarCombosFiltros();
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                CatalogoDataGrid.ItemsSource = null;
                EmptyStateTitleText.Text = "Falha ao carregar o catalogo";
                EmptyStateDescriptionText.Text = $"Nao foi possivel consultar o banco de dados do catalogo: {ex.Message}";
                EmptyStateBorder.Visibility = Visibility.Visible;
                CatalogoDataGrid.Visibility = Visibility.Collapsed;
                DetalhesItemText.Text = "Nenhum item carregado.";
                OrigemItemText.Text = "A consulta falhou antes de montar os detalhes.";
                ResumoCatalogoText.Text = "Tente atualizar novamente apos revisar o banco.";
                DicasUsoText.Text = "Se o erro persistir, valide as migracoes e a conexao SQLite antes de tentar uma nova importacao.";
            }
        }

        private void AtualizarCombosFiltros()
        {
            var marcaAtual = MarcaComboBox.SelectedItem as string;
            var categoriaAtual = CategoriaComboBox.SelectedItem as string;

            var marcas = _catalogoPecasService.ObterMarcas();
            marcas.Insert(0, "Todas as marcas");

            var categorias = _catalogoPecasService.ObterCategorias();
            categorias.Insert(0, "Todas as categorias");

            _suspendFilters = true;
            MarcaComboBox.ItemsSource = marcas;
            MarcaComboBox.SelectedItem = marcas.Contains(marcaAtual ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                ? marcaAtual
                : marcas[0];

            CategoriaComboBox.ItemsSource = categorias;
            CategoriaComboBox.SelectedItem = categorias.Contains(categoriaAtual ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                ? categoriaAtual
                : categorias[0];
            _suspendFilters = false;
        }

        private void AplicarFiltros()
        {
            if (_suspendFilters)
            {
                return;
            }

            var filtro = new CatalogoPecaFiltro
            {
                Termo = BuscaTextBox.Text ?? string.Empty,
                Marca = (MarcaComboBox.SelectedItem as string) is { Length: > 0 } marca && !string.Equals(marca, "Todas as marcas", StringComparison.OrdinalIgnoreCase)
                    ? marca
                    : string.Empty,
                Categoria = (CategoriaComboBox.SelectedItem as string) is { Length: > 0 } categoria && !string.Equals(categoria, "Todas as categorias", StringComparison.OrdinalIgnoreCase)
                    ? categoria
                    : string.Empty,
                StatusRevisao = (StatusComboBox.SelectedItem as string) is { Length: > 0 } status && !string.Equals(status, "Todos os status", StringComparison.OrdinalIgnoreCase)
                    ? status
                    : string.Empty,
                SomentePendentes = SomentePendentesCheckBox.IsChecked == true
            };

            var selecionado = CatalogoDataGrid.SelectedItem as CatalogoPeca;
            _itensFiltrados.Clear();
            _itensFiltrados.AddRange(_catalogoPecasService.Buscar(filtro));

            CatalogoDataGrid.ItemsSource = null;
            CatalogoDataGrid.ItemsSource = _itensFiltrados;

            AtualizarCards();
            AtualizarEstadoVazio();

            if (_itensFiltrados.Count > 0)
            {
                CatalogoDataGrid.SelectedItem = selecionado == null
                    ? _itensFiltrados[0]
                    : _itensFiltrados.FirstOrDefault(item => item.Id == selecionado.Id) ?? _itensFiltrados[0];
            }
            else
            {
                CatalogoDataGrid.SelectedItem = null;
            }

            AtualizarPainelDetalhes(CatalogoDataGrid.SelectedItem as CatalogoPeca);
            AtualizarEstadoAcoes();
        }

        private void AtualizarCards()
        {
            var resumo = _catalogoPecasService.ObterResumo();
            TotalItensText.Text = resumo.TotalItens.ToString(CultureInfo.InvariantCulture);
            PendentesText.Text = resumo.PendentesRevisao.ToString(CultureInfo.InvariantCulture);
            ConvertidosText.Text = resumo.ConvertidosEstoque.ToString(CultureInfo.InvariantCulture);
            DuplicadosText.Text = resumo.DuplicadosProvaveis.ToString(CultureInfo.InvariantCulture);
        }

        private void AtualizarEstadoVazio()
        {
            var existeHistorico = _todosItens.Count > 0;
            var existeResultado = _itensFiltrados.Count > 0;

            CatalogoDataGrid.Visibility = existeResultado ? Visibility.Visible : Visibility.Collapsed;
            EmptyStateBorder.Visibility = existeResultado ? Visibility.Collapsed : Visibility.Visible;

            if (existeResultado)
            {
                return;
            }

            if (!existeHistorico)
            {
                EmptyStateTitleText.Text = "Nenhum item no catalogo";
                EmptyStateDescriptionText.Text = "Importe um PDF, CSV, Excel ou XML para criar a base tecnica sem jogar tudo no estoque.";
                return;
            }

            EmptyStateTitleText.Text = "Nenhum item encontrado";
            EmptyStateDescriptionText.Text = "Ajuste busca, marca, categoria, status ou o filtro de pendencias.";
        }

        private void AtualizarPainelDetalhes(CatalogoPeca? item)
        {
            var referencia = item ?? _itensFiltrados.FirstOrDefault() ?? _todosItens.FirstOrDefault();
            if (referencia == null)
            {
                DetalhesItemText.Text = "Nenhum item selecionado.\nImporte um catalogo para iniciar a base tecnica.";
                OrigemItemText.Text = "Sem dados de origem no momento.";
                ResumoCatalogoText.Text = "O historico sera exibido aqui assim que a primeira importacao for confirmada.";
                DicasUsoText.Text = "1. Clique em Importar catalogo. 2. Gere a previa. 3. Revise o item antes de converter para estoque.";
                return;
            }

            DetalhesItemText.Text =
                $"Codigo: {TextoOuPadrao(referencia.CodigoFabricante)}\n" +
                $"Marca: {TextoOuPadrao(referencia.Marca)}\n" +
                $"Nome: {TextoOuPadrao(referencia.NomeExibicao)}\n" +
                $"Categoria: {TextoOuPadrao(referencia.Categoria)} / {TextoOuPadrao(referencia.Subcategoria)}\n" +
                $"Aplicacao: {TextoOuPadrao(referencia.Aplicacao)}\n" +
                $"Status: {TextoOuPadrao(referencia.StatusRevisao)}\n" +
                $"Vinculado ao estoque: {(referencia.EstaVinculadoAoEstoque ? "Sim" : "Nao")}";

            OrigemItemText.Text =
                $"Fonte: {TextoOuPadrao(referencia.FonteCatalogo)}\n" +
                $"Arquivo: {TextoOuPadrao(referencia.ArquivoOrigem)}\n" +
                $"Pagina: {TextoOuPadrao(referencia.PaginaCatalogo)}\n" +
                $"Observacoes: {TextoOuPadrao(referencia.ObservacoesTecnicas)}";

            var ultimaImportacao = _historicoImportacoes.FirstOrDefault();
            ResumoCatalogoText.Text =
                $"Importacoes registradas: {_historicoImportacoes.Count}\n" +
                $"Ultima importacao: {(ultimaImportacao == null ? "Nenhuma" : $"{ultimaImportacao.ArquivoNome} em {ultimaImportacao.DataImportacao:dd/MM/yyyy HH:mm}")}\n" +
                $"Resumo da ultima: {(ultimaImportacao == null ? "-" : ultimaImportacao.Resumo)}";

            DicasUsoText.Text =
                "Itens vindos de PDF entram pendentes por seguranca. " +
                "Use Revisar para limpar descricao, categoria e observacoes antes de criar o produto real no estoque.";
        }

        private void AtualizarEstadoAcoes()
        {
            var possuiSelecao = CatalogoDataGrid.SelectedItem is CatalogoPeca;
            CriarProdutoButton.IsEnabled = possuiSelecao && _permissionService.TemPermissaoCodigo("CATALOGO_CRIAR_PRODUTO");
            ImportarCatalogoButton.IsEnabled = _permissionService.TemPermissaoCodigo("CATALOGO_IMPORTAR");
            ExportarButton.IsEnabled = _permissionService.TemPermissaoCodigo("CATALOGO_EXPORTAR");
        }

        private void ImportarCatalogoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissionService.TemPermissaoCodigo("CATALOGO_IMPORTAR"))
            {
                ShowCatalogMessage("Sua sessao nao possui permissao para importar catalogos.", "Catalogo de Pecas", MessageBoxImage.Warning);
                return;
            }

            var window = new ImportarCatalogoPecasWindow();
            ConfigurarOwner(window);

            if (window.ShowDialog() == true)
            {
                CarregarDados();
            }
        }

        private void HistoricoImportacoesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_historicoImportacoes.Count == 0)
            {
                ShowCatalogMessage("Nenhuma importacao de catalogo foi registrada ainda.", "Historico", MessageBoxImage.Information);
                return;
            }

            var builder = new StringBuilder();
            foreach (var item in _historicoImportacoes.Take(15))
            {
                builder.AppendLine($"{item.DataImportacao:dd/MM/yyyy HH:mm} | {item.ArquivoNome} | {item.Status} | {item.Resumo}");
            }

            ShowCatalogMessage(builder.ToString(), "Historico de importacoes do catalogo", MessageBoxImage.Information);
        }

        private void ExportarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_permissionService.TemPermissaoCodigo("CATALOGO_EXPORTAR"))
            {
                ShowCatalogMessage("Sua sessao nao possui permissao para exportar o catalogo.", "Catalogo de Pecas", MessageBoxImage.Warning);
                return;
            }

            if (_itensFiltrados.Count == 0)
            {
                ShowCatalogMessage("Nao ha itens filtrados para exportar.", "Catalogo de Pecas", MessageBoxImage.Information);
                return;
            }

            var defaultDirectory = CatalogoWorkspacePaths.GetExportDirectory();
            var defaultFileName = $"catalogo_pecas_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var destino = Path.Combine(defaultDirectory, defaultFileName);

            if (!App.IsAutomatedTestMode)
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Arquivo CSV (*.csv)|*.csv",
                    FileName = defaultFileName,
                    InitialDirectory = defaultDirectory
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                destino = dialog.FileName;
            }

            _catalogoPecasService.ExportarCsv(_itensFiltrados, destino);
            ShowCatalogMessage($"Catalogo exportado com sucesso em:\n{destino}", "Catalogo de Pecas", MessageBoxImage.Information);
        }

        private void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarDados();
        }

        private void CriarProdutoButton_Click(object sender, RoutedEventArgs e)
        {
            CriarProdutoParaItem(CatalogoDataGrid.SelectedItem as CatalogoPeca);
        }

        private void VerButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObterItemDaLinha(sender) is not CatalogoPeca item)
            {
                return;
            }

            var window = new RevisarCatalogoPecaWindow(item, somenteLeitura: true);
            ConfigurarOwner(window);
            window.ShowDialog();
        }

        private void RevisarButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObterItemDaLinha(sender) is not CatalogoPeca item)
            {
                return;
            }

            if (!_permissionService.TemPermissaoCodigo("CATALOGO_REVISAR"))
            {
                ShowCatalogMessage("Sua sessao nao possui permissao para revisar itens do catalogo.", "Catalogo de Pecas", MessageBoxImage.Warning);
                return;
            }

            var window = new RevisarCatalogoPecaWindow(item);
            ConfigurarOwner(window);

            if (window.ShowDialog() == true)
            {
                CarregarDados();
            }
        }

        private void CriarProdutoLinhaButton_Click(object sender, RoutedEventArgs e)
        {
            CriarProdutoParaItem(ObterItemDaLinha(sender));
        }

        private void IgnorarButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObterItemDaLinha(sender) is not CatalogoPeca item)
            {
                return;
            }

            if (!_permissionService.TemPermissaoCodigo("CATALOGO_REVISAR"))
            {
                ShowCatalogMessage("Sua sessao nao possui permissao para alterar o status do catalogo.", "Catalogo de Pecas", MessageBoxImage.Warning);
                return;
            }

            var confirmar = ShowCatalogConfirmation(
                $"Deseja marcar o item '{item.CodigoFabricante}' como Ignorado?",
                "Catalogo de Pecas",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmar != MessageBoxResult.Yes)
            {
                return;
            }

            _catalogoPecasService.MarcarStatus(item.Id, "Ignorado");
            CarregarDados();
        }

        private void CriarProdutoParaItem(CatalogoPeca? item)
        {
            if (item == null)
            {
                ShowCatalogMessage("Selecione um item do catalogo primeiro.", "Catalogo de Pecas", MessageBoxImage.Information);
                return;
            }

            var produto = _catalogoParaProdutoService.IniciarConversao(ObterJanelaPaiDisponivel(), item);
            if (produto != null)
            {
                CarregarDados();
            }
        }

        private void LimparFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            _suspendFilters = true;
            BuscaTextBox.Text = string.Empty;
            MarcaComboBox.SelectedIndex = 0;
            CategoriaComboBox.SelectedIndex = 0;
            StatusComboBox.SelectedIndex = 0;
            SomentePendentesCheckBox.IsChecked = false;
            _suspendFilters = false;

            AplicarFiltros();
        }

        private void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void Filtros_Changed(object sender, EventArgs e)
        {
            AplicarFiltros();
        }

        private void CatalogoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarPainelDetalhes(CatalogoDataGrid.SelectedItem as CatalogoPeca);
            AtualizarEstadoAcoes();
        }

        private static CatalogoPeca? ObterItemDaLinha(object sender)
        {
            return (sender as FrameworkElement)?.DataContext as CatalogoPeca;
        }

        private void ConfigurarOwner(Window dialog)
        {
            var owner = ObterJanelaPaiDisponivel();
            if (owner != null && owner != dialog)
            {
                dialog.Owner = owner;
            }
        }

        private Window? ObterJanelaPaiDisponivel()
        {
            var owner = Window.GetWindow(this);
            return owner != null && owner.IsLoaded && owner.IsVisible ? owner : null;
        }

        private void ShowCatalogMessage(string message, string title, MessageBoxImage image)
        {
            var owner = ObterJanelaPaiDisponivel();
            if (owner != null)
            {
                MessageBox.Show(owner, message, title, MessageBoxButton.OK, image);
                return;
            }

            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        }

        private MessageBoxResult ShowCatalogConfirmation(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            var owner = ObterJanelaPaiDisponivel();
            return owner != null
                ? MessageBox.Show(owner, message, title, buttons, image)
                : MessageBox.Show(message, title, buttons, image);
        }

        private static string TextoOuPadrao(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }
    }
}
