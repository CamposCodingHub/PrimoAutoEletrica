using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using PrimoAutoEletrica.Helpers;
namespace PrimoAutoEletrica.UserControls
{
    public partial class FornecedoresControl : UserControl
    {
        private readonly IFornecedorRepository _fornecedorRepository;
        private readonly PermissionService _permissionService;
        private readonly FornecedoresViewModel _viewModel;
        private List<Fornecedor> _todosFornecedores = new();

        public FornecedoresControl()
        {
            InitializeComponent();
            _fornecedorRepository = global::PrimoAutoEletrica.App.Repositories.Fornecedores;
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger);
            _viewModel = App.Services.GetRequiredService<FornecedoresViewModel>();
            DataContext = _viewModel;

            CarregarFornecedores();
            AtualizarEstadoBotaoExcluir();
            Loaded += FornecedoresControl_Loaded;
        }

        private void FornecedoresControl_Loaded(object sender, RoutedEventArgs e)
        {
            RecarregarFornecedores();
        }

        private void RecarregarFornecedores()
        {
            CarregarFornecedores();
            AplicarFiltros();
            AtualizarEstadoBotaoExcluir();
        }

        private void CarregarFornecedores()
        {
            try
            {
                _todosFornecedores = _fornecedorRepository.ObterTodos();

                if (TotalFornecedoresText != null)
                    AtualizarIndicadores();

                if (FornecedoresDataGrid != null)
                {
                    FornecedoresDataGrid.ItemsSource = null;
                    FornecedoresDataGrid.ItemsSource = _todosFornecedores.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao carregar fornecedores: {ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AtualizarIndicadores()
        {
            if (TotalFornecedoresText != null)
                TotalFornecedoresText.Text = _todosFornecedores.Count.ToString();

            if (FornecedoresAtivosText != null)
                FornecedoresAtivosText.Text = _todosFornecedores.Count(f => f.Ativo).ToString();

            if (FornecedoresInativosText != null)
                FornecedoresInativosText.Text = _todosFornecedores.Count(f => !f.Ativo).ToString();

            if (MelhorAvaliadoText != null)
            {
                var fornecedoresAtivos = _todosFornecedores.Where(f => f.Ativo).ToList();
                var melhorAvaliado = fornecedoresAtivos
                    .OrderByDescending(f => f.Nota)
                    .ThenByDescending(f => f.TotalCompras)
                    .ThenByDescending(f => f.UltimaCompra ?? DateTime.MinValue)
                    .FirstOrDefault();

                MelhorAvaliadoText.Text = melhorAvaliado != null
                    ? $"{melhorAvaliado.Nota}/5"
                    : "0";
            }
        }

        private void AplicarFiltros()
        {
            if (FornecedoresDataGrid == null)
                return;

            var busca = BuscaTextBox?.Text?.Trim().ToUpperInvariant() ?? string.Empty;
            var filtroCategoria = ObterTextoComboBox(FiltroCategoriaCombo, "Todas as categorias");
            var filtroStatus = ObterTextoComboBox(FiltroStatusCombo, "Todos os status");

            var resultados = _todosFornecedores.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(busca))
            {
                resultados = resultados.Where(f =>
                    Contem(f.RazaoSocial, busca) ||
                    Contem(f.NomeFantasia, busca) ||
                    Contem(f.CNPJ, busca) ||
                    Contem(f.Cidade, busca) ||
                    Contem(f.Telefone, busca));
            }

            if (filtroCategoria != "Todas as categorias")
            {
                resultados = resultados.Where(f => f.Categoria == filtroCategoria);
            }

            if (filtroStatus == "Ativo")
            {
                resultados = resultados.Where(f => f.Ativo);
            }
            else if (filtroStatus == "Inativo")
            {
                resultados = resultados.Where(f => !f.Ativo);
            }

            FornecedoresDataGrid.ItemsSource = resultados.ToList();
            AtualizarEstadoBotaoExcluir();
        }

        private static bool Contem(string? valor, string busca)
        {
            return !string.IsNullOrWhiteSpace(valor)
                && valor.Contains(busca, StringComparison.OrdinalIgnoreCase);
        }

        private static string ObterTextoComboBox(ComboBox comboBox, string valorPadrao)
        {
            if (comboBox?.SelectedItem == null)
                return valorPadrao;

            if (comboBox.SelectedItem is ComboBoxItem item)
                return item.Content.ToString() ?? valorPadrao;

            return valorPadrao;
        }

        private void NovoFornecedorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("FORNECEDORES_CRIAR", "Voce nao possui permissao para cadastrar fornecedores."))
                    return;

                var novoFornecedorWindow = new Views.NovoFornecedorWindow();

                if (novoFornecedorWindow.ShowDialog() == true)
                {
                    CarregarFornecedores();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao abrir janela de novo fornecedor: {ex.Message}",
                    UiText.T("Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            RecarregarFornecedores();
        }

        private void FornecedoresDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarEstadoBotaoExcluir();
        }

        private void VisualizarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Fornecedor fornecedor)
            {
                try
                {
                    var fornecedorCompleto = _fornecedorRepository.ObterPorId(fornecedor.Id) ?? fornecedor;
                    var visualizarFornecedorWindow = new Views.VisualizarFornecedorWindow(fornecedorCompleto);
                    visualizarFornecedorWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erro ao visualizar fornecedor: {ex.Message}",
                        UiText.T("Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("FORNECEDORES_EDITAR", "Voce nao possui permissao para editar fornecedores."))
                return;

            if (sender is Button button && button.Tag is Fornecedor fornecedor)
            {
                try
                {
                    var fornecedorCompleto = _fornecedorRepository.ObterPorId(fornecedor.Id) ?? fornecedor;
                    var editarFornecedorWindow = new Views.EditarFornecedorWindow(fornecedorCompleto);

                    if (editarFornecedorWindow.ShowDialog() == true)
                    {
                        CarregarFornecedores();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erro ao editar fornecedor: {ex.Message}",
                        UiText.T("Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void ExcluirFornecedorSelecionadoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarPermissao("FORNECEDORES_EXCLUIR", "Voce nao possui permissao para excluir fornecedores."))
                return;

            if (FornecedoresDataGrid?.SelectedItem is not Fornecedor fornecedor)
            {
                MessageBox.Show(
                    "Selecione um fornecedor na planilha antes de excluir.",
                    "Fornecedor nao selecionado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            ExcluirFornecedor(fornecedor);
        }

        private void ExcluirFornecedor(Fornecedor fornecedor)
        {
            if (CriticalActionDialogService.ConfirmarExclusao(
                Window.GetWindow(this),
                "fornecedor",
                fornecedor.NomeFantasia,
                $"Razao social: {fornecedor.RazaoSocial}\nCNPJ: {fornecedor.CNPJ}\nCategoria: {fornecedor.Categoria}",
                "O cadastro do fornecedor e seus contatos associados serao removidos do banco local. Esta acao nao possui desfazer automatico."))
            {
                try
                {
                    _fornecedorRepository.Excluir(fornecedor.Id);
                    App.Audit.RegistrarAcaoCritica(
                        "Fornecedores",
                        "ExcluirFornecedor",
                        "Fornecedor",
                        fornecedor.Id.ToString(),
                        $"NomeFantasia={fornecedor.NomeFantasia}; CNPJ={fornecedor.CNPJ}");
                    RecarregarFornecedores();

                    MessageBox.Show(
                        UiText.T("SupplierDeleted"),
                        UiText.T("Success"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Erro ao excluir fornecedor: {ex.Message}",
                        UiText.T("Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void AtualizarEstadoBotaoExcluir()
        {
            if (ExcluirFornecedorSelecionadoButton == null)
            {
                return;
            }

            var temFornecedorSelecionado = FornecedoresDataGrid?.SelectedItem is Fornecedor;
            var podeExcluir = _permissionService.TemPermissaoCodigo("FORNECEDORES_EXCLUIR");
            ExcluirFornecedorSelecionadoButton.IsEnabled = temFornecedorSelecionado && podeExcluir;
            ExcluirFornecedorSelecionadoButton.ToolTip = podeExcluir
                ? "Selecione um fornecedor na planilha para excluir apenas esse cadastro."
                : "Sua sessao nao possui permissao para excluir fornecedores.";
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
                return true;

            MessageBox.Show(mensagem, UiText.T("AccessDenied"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        private void LimparFiltrosButton_Click(object sender, RoutedEventArgs e)
        {
            if (BuscaTextBox != null)
                BuscaTextBox.Text = string.Empty;

            if (FiltroCategoriaCombo != null)
                FiltroCategoriaCombo.SelectedIndex = 0;

            if (FiltroStatusCombo != null)
                FiltroStatusCombo.SelectedIndex = 0;

            AplicarFiltros();
        }

        private void BuscaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void FiltroCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            AplicarFiltros();
        }
    }
}
