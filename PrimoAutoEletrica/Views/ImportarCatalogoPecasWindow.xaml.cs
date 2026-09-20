using Microsoft.Win32;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Catalogo;
using System;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class ImportarCatalogoPecasWindow : Window
    {
        private readonly CatalogoImportacaoService _catalogoImportacaoService;
        private CatalogoImportacaoPreview? _previewAtual;

        public CatalogoImportacao? ImportacaoConfirmada { get; private set; }

        public ImportarCatalogoPecasWindow()
        {
            InitializeComponent();
            _catalogoImportacaoService = new CatalogoImportacaoService();
            ConfigurarCombos();
        }

        private void ConfigurarCombos()
        {
            TipoArquivoComboBox.ItemsSource = CatalogoArquivoSupport.TiposArquivoCombo;
            TipoArquivoComboBox.SelectedIndex = 0;
            FonteCatalogoTextBox.Text = string.Empty;
            MarcaTextBox.Text = string.Empty;
        }

        private void SelecionarArquivoButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Selecionar catalogo (PDF, imagem, planilha ou XML)",
                Filter = CatalogoArquivoSupport.FiltroDialogo,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false,
                AddExtension = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            ArquivoTextBox.Text = dialog.FileName;
            PreencherCamposSugestao(dialog.FileName);
        }

        private void GerarPreviaButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ValidarEntrada();
                GerarPreviaButton.IsEnabled = false;
                ConfirmarImportacaoButton.IsEnabled = false;

                _previewAtual = _catalogoImportacaoService.CriarPreviaImportacao(
                    ArquivoTextBox.Text.Trim(),
                    TipoArquivoComboBox.SelectedItem as string,
                    FonteCatalogoTextBox.Text,
                    MarcaTextBox.Text);

                PreviewDataGrid.ItemsSource = _previewAtual.Itens;
                ErrosDataGrid.ItemsSource = _previewAtual.Erros;
                TotalLidosText.Text = _previewAtual.TotalItens.ToString();
                TotalValidosText.Text = _previewAtual.TotalValidos.ToString();
                TotalDuplicadosText.Text = _previewAtual.TotalDuplicadosProvaveis.ToString();
                TotalErrosText.Text = _previewAtual.TotalComErro.ToString();
                TotalIncompletosText.Text = _previewAtual.TotalIncompletos.ToString();
                TotalSemNomeRealText.Text = _previewAtual.TotalSemNomeReal.ToString();
                AtualizarAlertaQualidade(_previewAtual);
                ConfirmarImportacaoButton.IsEnabled = _previewAtual.Itens.Count > 0;

                if (!string.IsNullOrWhiteSpace(_previewAtual.MarcaDetectada) &&
                    !string.Equals(MarcaTextBox.Text.Trim(), _previewAtual.MarcaDetectada, StringComparison.OrdinalIgnoreCase))
                {
                    MarcaTextBox.Text = _previewAtual.MarcaDetectada;
                }

                if (!string.IsNullOrWhiteSpace(_previewAtual.FonteDetectada))
                {
                    FonteCatalogoTextBox.Text = _previewAtual.FonteDetectada;
                }
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Falha ao gerar a previa do catalogo:\n{ex.Message}",
                    "Catalogo",
                    MessageBoxImage.Error,
                    "Catalogo",
                    ex);
            }
            finally
            {
                GerarPreviaButton.IsEnabled = true;
            }
        }

        private void ConfirmarImportacaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_previewAtual == null)
            {
                WindowInteractionHelper.ShowMessage(
                    "Gere a previa antes de confirmar a importacao.",
                    "Catalogo",
                    MessageBoxImage.Information,
                    "Catalogo");
                return;
            }

            if (_previewAtual.ImportacaoArriscada)
            {
                var confirmarArriscada = MessageBox.Show(
                    this,
                    $"{_previewAtual.AlertaQualidade}\n\nDeseja importar mesmo assim?",
                    "Catalogo de Pecas - qualidade baixa",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirmarArriscada != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            try
            {
                ConfirmarImportacaoButton.IsEnabled = false;
                ImportacaoConfirmada = _catalogoImportacaoService.ConfirmarImportacao(_previewAtual);

                WindowInteractionHelper.ShowMessage(
                    $"Importacao concluida.\n\nStatus: {ImportacaoConfirmada.Status}\nImportados: {ImportacaoConfirmada.TotalImportados}\nDuplicados: {ImportacaoConfirmada.TotalDuplicados}\nErros: {ImportacaoConfirmada.TotalComErro}",
                    "Catalogo",
                    MessageBoxImage.Information,
                    "Catalogo");

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Catalogo");
            }
            catch (Exception ex)
            {
                ConfirmarImportacaoButton.IsEnabled = true;
                WindowInteractionHelper.ShowMessage(
                    $"Falha ao confirmar a importacao:\n{ex.Message}",
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

        private void ValidarEntrada()
        {
            CatalogoArquivoSupport.ValidarArquivo(ArquivoTextBox.Text);

            if (string.IsNullOrWhiteSpace(FonteCatalogoTextBox.Text))
            {
                throw new InvalidOperationException("Informe a fonte do catalogo.");
            }

            if (string.IsNullOrWhiteSpace(MarcaTextBox.Text))
            {
                throw new InvalidOperationException("Informe a marca do catalogo.");
            }
        }

        private void PreencherCamposSugestao(string caminhoArquivo)
        {
            var (marca, fonte) = CatalogoMarcaDetector.ResolverMarcaEFonte(
                caminhoArquivo,
                marcaInformada: null,
                fonteInformada: null);

            MarcaTextBox.Text = marca;
            FonteCatalogoTextBox.Text = fonte;
        }

        private void AtualizarAlertaQualidade(CatalogoImportacaoPreview preview)
        {
            if (string.IsNullOrWhiteSpace(preview.AlertaQualidade))
            {
                AlertaQualidadeBorder.Visibility = Visibility.Collapsed;
                AlertaQualidadeText.Text = string.Empty;
                return;
            }

            AlertaQualidadeBorder.Visibility = Visibility.Visible;
            AlertaQualidadeText.Text = preview.AlertaQualidade;
        }
    }
}
