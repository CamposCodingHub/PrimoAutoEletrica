using Microsoft.Win32;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimoAutoEletrica.Views
{
    public partial class EditarProdutoWindow : Window
    {
        private readonly Produto _produto;
        private readonly PermissionService _permissionService;
        private readonly RecordLockService _recordLockService;
        private bool _lockObtido;
        private string _selectedImagePath = string.Empty;
        private string _currentImagePath = string.Empty;
        private bool _removeImageRequested;
        private readonly List<string> _attachmentPaths = new();

        public EditarProdutoWindow(Produto produto)
        {
            InitializeComponent();
            _produto = App.Repositories.Produtos.ObterPorId(produto.Id) ?? produto;
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger);
            _recordLockService = new RecordLockService(App.Database, App.Logger, App.Session, App.Audit);

            Loaded += EditarProdutoWindow_Loaded;
            Closed += EditarProdutoWindow_Closed;

            CarregarDadosProduto();
            AtualizarEstadoPerecivel();
            AtualizarIndicadoresFinanceiros();
            AtualizarAlertas();
        }

        private void EditarProdutoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var lockResult = _recordLockService.TryLock("Produto", _produto.Id.ToString(), _produto.Nome);
            if (!lockResult.Success)
            {
                MessageBox.Show(
                    lockResult.Message,
                    UiText.T("RecordBlocked"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                Close();
                return;
            }

            _lockObtido = true;
        }

        private void EditarProdutoWindow_Closed(object? sender, EventArgs e)
        {
            if (_lockObtido)
            {
                _recordLockService.ReleaseLock("Produto", _produto.Id.ToString());
            }
        }

        private void CarregarDadosProduto()
        {
            CodigoTextBox.Text = _produto.Codigo;
            NomeTextBox.Text = _produto.Nome;
            DescricaoTextBox.Text = _produto.Descricao;
            SelecionarComboBoxPorTexto(CategoriaComboBox, _produto.Categoria);
            SelecionarComboBoxPorTexto(UnidadeMedidaComboBox, _produto.UnidadeMedida);
            MarcaTextBox.Text = _produto.Marca;
            ModeloTextBox.Text = _produto.Modelo;
            FornecedorTextBox.Text = _produto.Fornecedor;
            CnpjFornecedorTextBox.Text = _produto.CNPJFornecedor;
            ContatoFornecedorTextBox.Text = _produto.ContatoFornecedor;
            TelefoneFornecedorTextBox.Text = _produto.TelefoneFornecedor;
            QuantidadeTextBox.Text = _produto.QuantidadeEstoque.ToString();
            QuantidadeMinimaTextBox.Text = _produto.QuantidadeMinima.ToString();
            QuantidadeMaximaTextBox.Text = _produto.QuantidadeMaxima.ToString();
            LocalizacaoTextBox.Text = _produto.Localizacao;
            PrateleiraTextBox.Text = _produto.Prateleira;
            GavetaTextBox.Text = _produto.Gaveta;
            PrecoCompraTextBox.Text = _produto.PrecoCompra.ToString("N2");
            PrecoVendaTextBox.Text = _produto.PrecoVenda.ToString("N2");
            PesoTextBox.Text = _produto.Peso;
            DimensoesTextBox.Text = _produto.Dimensoes;
            CorTextBox.Text = _produto.Cor;
            MaterialTextBox.Text = _produto.Material;
            CodigoBarrasTextBox.Text = _produto.CodigoBarras;
            SkuTextBox.Text = _produto.SKU;
            NcmsTextBox.Text = _produto.NCMS;
            CestTextBox.Text = _produto.CEST;
            CfopTextBox.Text = _produto.CFOP;
            AtivoCheckBox.IsChecked = _produto.Ativo;
            ProdutoPerecivelCheckBox.IsChecked = _produto.ProdutoPerecivel;
            DataFabricacaoDatePicker.SelectedDate = _produto.DataFabricacao;
            DataValidadeDatePicker.SelectedDate = _produto.DataValidade;
            LoteTextBox.Text = _produto.Lote;
            ObservacoesTextBox.Text = _produto.Observacoes;
            CodigoAuxiliarTextBox.Text = _produto.Id.ToString("N")[..12].ToUpperInvariant();

            _currentImagePath = ProdutoMediaService.ResolveExistingPath(_produto.ImagemUrl);
            AtualizarPreviewFoto(_currentImagePath);

            _attachmentPaths.Clear();
            _attachmentPaths.AddRange(ProdutoMediaService.DeserializeAttachmentPaths(_produto.Anexos)
                .Select(path => ProdutoMediaService.ResolveExistingPath(path))
                .Where(path => !string.IsNullOrWhiteSpace(path)));
            AtualizarListaAnexos();
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SelecionarFotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    "Troca de foto do produto validada em automacao sem abrir seletor de arquivos.",
                    "Estoque");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = ProdutoMediaService.SupportedImageFilter
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            if (!ProdutoMediaService.IsSupportedImageFile(dialog.FileName))
            {
                WindowInteractionHelper.ShowMessage(
                    UiText.T("SelectValidImage"),
                    UiText.T("Image"),
                    MessageBoxImage.Warning,
                    "Estoque");
                return;
            }

            _selectedImagePath = dialog.FileName;
            _removeImageRequested = false;
            AtualizarPreviewFoto(_selectedImagePath);
        }

        private void RemoverFotoButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedImagePath = string.Empty;
            _removeImageRequested = true;
            AtualizarPreviewFoto(string.Empty);
        }

        private void SelecionarAnexosButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    "Selecao de anexos do produto validada em automacao sem abrir seletor de arquivos.",
                    "Estoque");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = ProdutoMediaService.SupportedAttachmentFilter,
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            foreach (var fileName in dialog.FileNames)
            {
                if (!ProdutoMediaService.IsSupportedAttachmentFile(fileName))
                {
                    WindowInteractionHelper.ShowMessage(
                        $"Anexo ignorado por tipo nao suportado:\n{fileName}",
                        UiText.T("Attachment"),
                        MessageBoxImage.Warning,
                        "Estoque");
                    continue;
                }

                AdicionarAnexo(fileName);
            }

            AtualizarListaAnexos();
        }

        private void AbrirAnexoButton_Click(object sender, RoutedEventArgs e)
        {
            if (AnexosListBox.SelectedItem is not string path)
            {
                WindowInteractionHelper.ShowMessage(UiText.T("SelectAttachmentOpen"), UiText.T("Attachment"), MessageBoxImage.Information, "Estoque");
                return;
            }

            if (App.IsAutomatedTestMode)
            {
                WindowInteractionHelper.LogAutomationExternalAction(
                    $"Abertura de anexo do produto validada em automacao: {path}",
                    "Estoque");
                return;
            }

            var resolvedPath = ProdutoMediaService.ResolveExistingPath(path);
            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                WindowInteractionHelper.ShowMessage(UiText.T("AttachmentMissing"), UiText.T("Attachment"), MessageBoxImage.Warning, "Estoque");
                return;
            }

            Process.Start(new ProcessStartInfo(resolvedPath) { UseShellExecute = true });
        }

        private void RemoverAnexoButton_Click(object sender, RoutedEventArgs e)
        {
            if (AnexosListBox.SelectedItem is not string path)
            {
                WindowInteractionHelper.ShowMessage(UiText.T("SelectAttachmentRemove"), UiText.T("Attachment"), MessageBoxImage.Information, "Estoque");
                return;
            }

            _attachmentPaths.RemoveAll(candidate => string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase));
            AtualizarListaAnexos();
        }

        private void GerarCodigoButton_Click(object sender, RoutedEventArgs e)
        {
            CodigoBarrasTextBox.Text = GerarCodigoBarras();
        }

        private void CalculoCampo_TextChanged(object sender, TextChangedEventArgs e)
        {
            AtualizarIndicadoresFinanceiros();
            AtualizarAlertas();
        }

        private void ProdutoPerecivelCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            AtualizarEstadoPerecivel();
            AtualizarAlertas();
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidarPermissao("ESTOQUE_EDITAR", "Voce nao possui permissao para editar produtos."))
                {
                    return;
                }

                if (!TryReadFormValues(out var formValues))
                {
                    return;
                }

                if (PrecoFoiAlterado(formValues.PrecoCompra, formValues.PrecoVenda)
                    && !ValidarPermissao("ESTOQUE_AJUSTAR_PRECO", "Voce nao possui permissao para alterar precos de produtos."))
                {
                    return;
                }

                _produto.Codigo = string.IsNullOrWhiteSpace(CodigoTextBox.Text) ? _produto.Codigo : CodigoTextBox.Text.Trim();
                _produto.Nome = NomeTextBox.Text.Trim();
                _produto.Descricao = DescricaoTextBox.Text.Trim();
                _produto.Categoria = ObterTextoComboBox(CategoriaComboBox);
                _produto.UnidadeMedida = ObterTextoComboBox(UnidadeMedidaComboBox);
                _produto.Marca = MarcaTextBox.Text.Trim();
                _produto.Modelo = ModeloTextBox.Text.Trim();
                _produto.Fornecedor = FornecedorTextBox.Text.Trim();
                _produto.CNPJFornecedor = CnpjFornecedorTextBox.Text.Trim();
                _produto.ContatoFornecedor = ContatoFornecedorTextBox.Text.Trim();
                _produto.TelefoneFornecedor = TelefoneFornecedorTextBox.Text.Trim();
                _produto.QuantidadeEstoque = formValues.Quantidade;
                _produto.QuantidadeMinima = formValues.QuantidadeMinima;
                _produto.QuantidadeMaxima = formValues.QuantidadeMaxima;
                _produto.Localizacao = LocalizacaoTextBox.Text.Trim();
                _produto.Prateleira = PrateleiraTextBox.Text.Trim();
                _produto.Gaveta = GavetaTextBox.Text.Trim();
                _produto.PrecoCompra = formValues.PrecoCompra;
                _produto.PrecoVenda = formValues.PrecoVenda;
                _produto.MargemLucro = CalcularMargem(formValues.PrecoCompra, formValues.PrecoVenda);
                _produto.ValorTotalEstoque = formValues.Quantidade * formValues.PrecoCompra;
                _produto.Peso = PesoTextBox.Text.Trim();
                _produto.Dimensoes = DimensoesTextBox.Text.Trim();
                _produto.Cor = CorTextBox.Text.Trim();
                _produto.Material = MaterialTextBox.Text.Trim();
                _produto.CodigoBarras = CodigoBarrasTextBox.Text.Trim();
                _produto.SKU = SkuTextBox.Text.Trim();
                _produto.NCMS = NcmsTextBox.Text.Trim();
                _produto.CEST = CestTextBox.Text.Trim();
                _produto.CFOP = CfopTextBox.Text.Trim();
                _produto.Ativo = AtivoCheckBox.IsChecked == true;
                _produto.ProdutoPerecivel = ProdutoPerecivelCheckBox.IsChecked == true;
                _produto.DataFabricacao = DataFabricacaoDatePicker.SelectedDate;
                _produto.DataValidade = DataValidadeDatePicker.SelectedDate;
                _produto.Lote = LoteTextBox.Text.Trim();
                _produto.Observacoes = ObservacoesTextBox.Text.Trim();
                _produto.ImagemUrl = ResolverImagemPersistida();
                var anexosAnteriores = _produto.Anexos;
                _produto.Anexos = ProdutoMediaService.PersistSelectedAttachments(_attachmentPaths, _produto.Id, NomeTextBox.Text.Trim());
                _produto.DataUltimaAtualizacao = DateTime.Now;

                App.Repositories.Produtos.Atualizar(_produto);
                ProdutoMediaService.DeleteManagedAttachmentsNotIn(anexosAnteriores, _produto.Anexos);

                WindowInteractionHelper.ShowMessage(
                    "Produto atualizado com sucesso!",
                    UiText.T("Success"),
                    MessageBoxImage.Information,
                    "Estoque");

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Estoque");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao salvar produto:\n{ex.Message}",
                    UiText.T("Error"),
                    MessageBoxImage.Error,
                    "Estoque",
                    ex);
            }
        }

        private string ResolverImagemPersistida()
        {
            if (_removeImageRequested)
            {
                ProdutoMediaService.DeleteManagedImageIfOwned(_currentImagePath);
                _currentImagePath = string.Empty;
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(_selectedImagePath))
            {
                return _currentImagePath;
            }

            var novaImagem = ProdutoMediaService.PersistSelectedImage(_selectedImagePath, _produto.Id, NomeTextBox.Text.Trim());
            if (!string.IsNullOrWhiteSpace(_currentImagePath)
                && !string.Equals(_currentImagePath, novaImagem, StringComparison.OrdinalIgnoreCase))
            {
                ProdutoMediaService.DeleteManagedImageIfOwned(_currentImagePath);
            }

            _currentImagePath = novaImagem;
            _selectedImagePath = string.Empty;
            return novaImagem;
        }

        private void AtualizarPreviewFoto(string path)
        {
            var preview = ProdutoMediaService.TryCreatePreviewSource(path);
            if (preview == null)
            {
                FotoPreviewImage.Source = null;
                FotoPreviewImage.Visibility = Visibility.Collapsed;
                FotoPlaceholderBorder.Visibility = Visibility.Visible;
                return;
            }

            FotoPreviewImage.Source = preview;
            FotoPreviewImage.Visibility = Visibility.Visible;
            FotoPlaceholderBorder.Visibility = Visibility.Collapsed;
        }

        private void AdicionarAnexo(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || _attachmentPaths.Any(candidate => string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            _attachmentPaths.Add(path);
        }

        private void AtualizarListaAnexos()
        {
            _attachmentPaths.RemoveAll(string.IsNullOrWhiteSpace);
            AnexosListBox.ItemsSource = null;
            AnexosListBox.ItemsSource = _attachmentPaths.ToList();
            AnexosCountTextBlock.Text = _attachmentPaths.Count == 0
                ? "Nenhum anexo selecionado"
                : $"{_attachmentPaths.Count} anexo(s) selecionado(s)";
        }

        private void AtualizarEstadoPerecivel()
        {
            var enabled = ProdutoPerecivelCheckBox.IsChecked == true;
            DataFabricacaoDatePicker.IsEnabled = enabled;
            DataValidadeDatePicker.IsEnabled = enabled;

            if (!enabled)
            {
                DataFabricacaoDatePicker.SelectedDate = null;
                DataValidadeDatePicker.SelectedDate = null;
            }
        }

        private void AtualizarIndicadoresFinanceiros()
        {
            var quantidade = TryParseIntOrZero(QuantidadeTextBox.Text, out var q) ? q : 0;
            var precoCompra = TryParseDecimalOrZero(PrecoCompraTextBox.Text, out var pc) ? pc : 0m;
            var precoVenda = TryParseDecimalOrZero(PrecoVendaTextBox.Text, out var pv) ? pv : 0m;
            var margem = CalcularMargem(precoCompra, precoVenda);

            MargemLucroTextBlock.Text = $"{margem:N2}%";
            ValorTotalEstoqueTextBlock.Text = (quantidade * precoCompra).ToString("C2");
            CodigoAuxiliarTextBox.Text = _produto.Id.ToString("N")[..12].ToUpperInvariant();

            if (margem < 0)
            {
                MargemLucroTextBlock.Foreground = (Brush)FindResource("DangerBrush");
            }
            else if (margem < 15)
            {
                MargemLucroTextBlock.Foreground = (Brush)FindResource("WarningBrush");
            }
            else
            {
                MargemLucroTextBlock.Foreground = (Brush)FindResource("SuccessBrush");
            }
        }

        private void AtualizarAlertas()
        {
            var alertas = new List<string>();
            var quantidadeMinima = TryParseIntOrZero(QuantidadeMinimaTextBox.Text, out var minima) ? minima : 0;
            var quantidadeMaxima = TryParseIntOrZero(QuantidadeMaximaTextBox.Text, out var maxima) ? maxima : 0;
            var precoCompra = TryParseDecimalOrZero(PrecoCompraTextBox.Text, out var compra) ? compra : 0m;
            var precoVenda = TryParseDecimalOrZero(PrecoVendaTextBox.Text, out var venda) ? venda : 0m;
            var margem = CalcularMargem(compra, venda);

            if (precoVenda > 0 && precoVenda < precoCompra)
            {
                alertas.Add("Preco de venda abaixo do preco de compra.");
            }

            if (quantidadeMaxima > 0 && quantidadeMinima > quantidadeMaxima)
            {
                alertas.Add("Estoque minimo maior que o estoque maximo.");
            }

            if (margem < 0)
            {
                alertas.Add("Margem negativa detectada para o produto.");
            }

            if (ProdutoPerecivelCheckBox.IsChecked == true && !DataValidadeDatePicker.SelectedDate.HasValue)
            {
                alertas.Add("Produto perecivel sem data de validade informada.");
            }

            AlertBorder.Visibility = alertas.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            AlertTextBlock.Text = string.Join(" ", alertas);
        }

        private bool TryReadFormValues(out ProdutoFormValues values)
        {
            values = default;
            var quantidade = 0;
            var quantidadeMinima = 0;
            var quantidadeMaxima = 0;
            var precoCompra = 0m;
            var precoVenda = 0m;

            if (string.IsNullOrWhiteSpace(NomeTextBox.Text))
            {
                return ExibirErroValidacao("O nome do produto e obrigatorio.", NomeTextBox, out values);
            }

            if (!TryParseIntOrZero(QuantidadeTextBox.Text, out quantidade))
            {
                return ExibirErroValidacao("Informe uma quantidade valida para o estoque.", QuantidadeTextBox, out values);
            }

            if (!TryParseIntOrZero(QuantidadeMinimaTextBox.Text, out quantidadeMinima) || quantidadeMinima < 0)
            {
                return ExibirErroValidacao("Informe uma quantidade minima valida.", QuantidadeMinimaTextBox, out values);
            }

            if (!TryParseIntOrZero(QuantidadeMaximaTextBox.Text, out quantidadeMaxima) || quantidadeMaxima < 0)
            {
                return ExibirErroValidacao("Informe uma quantidade maxima valida.", QuantidadeMaximaTextBox, out values);
            }

            if (quantidadeMaxima > 0 && quantidadeMaxima < quantidadeMinima)
            {
                return ExibirErroValidacao("A quantidade maxima nao pode ser menor que a minima.", QuantidadeMaximaTextBox, out values);
            }

            if (!TryParseDecimalOrZero(PrecoCompraTextBox.Text, out precoCompra) || precoCompra < 0)
            {
                return ExibirErroValidacao("Informe um preco de compra valido.", PrecoCompraTextBox, out values);
            }

            if (!TryParseDecimalOrZero(PrecoVendaTextBox.Text, out precoVenda) || precoVenda < 0)
            {
                return ExibirErroValidacao("Informe um preco de venda valido.", PrecoVendaTextBox, out values);
            }

            if (!ValidarEstoqueNegativo(quantidade))
            {
                return false;
            }

            if (ProdutoPerecivelCheckBox.IsChecked == true &&
                DataFabricacaoDatePicker.SelectedDate.HasValue &&
                DataValidadeDatePicker.SelectedDate.HasValue &&
                DataValidadeDatePicker.SelectedDate.Value < DataFabricacaoDatePicker.SelectedDate.Value)
            {
                return ExibirErroValidacao("A validade nao pode ser anterior a data de fabricacao.", DataValidadeDatePicker, out values);
            }

            values = new ProdutoFormValues
            {
                Quantidade = quantidade,
                QuantidadeMinima = quantidadeMinima,
                QuantidadeMaxima = quantidadeMaxima,
                PrecoCompra = precoCompra,
                PrecoVenda = precoVenda
            };

            return true;
        }

        private bool ExibirErroValidacao(string mensagem, Control control, out ProdutoFormValues values)
        {
            values = default;
            WindowInteractionHelper.ShowMessage(
                mensagem,
                UiText.T("Validation"),
                MessageBoxImage.Warning,
                "Estoque");
            control.Focus();
            return false;
        }

        private bool PrecoFoiAlterado(decimal precoCompra, decimal precoVenda)
        {
            return precoCompra != _produto.PrecoCompra || precoVenda != _produto.PrecoVenda;
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            WindowInteractionHelper.ShowMessage(mensagem, UiText.T("AccessDenied"), MessageBoxImage.Warning, "Estoque");
            return false;
        }

        private bool ValidarEstoqueNegativo(int quantidade)
        {
            if (quantidade >= 0)
            {
                return true;
            }

            if (!ValidarPermissao("ESTOQUE_PERMITIR_NEGATIVO", "Estoque negativo exige permissao de gerente ou administrador."))
            {
                return false;
            }

            if (_produto.QuantidadeEstoque == quantidade)
            {
                return true;
            }

            return CriticalActionDialogService.ConfirmarAcao(
                this,
                new CriticalActionRequest
                {
                    WindowTitle = "Confirmar estoque negativo",
                    Header = "Edicao com estoque negativo",
                    Summary = $"Voce esta prestes a salvar o produto com estoque negativo ({quantidade}).",
                    Details = $"Produto: {_produto.Nome}\nCodigo: {_produto.Codigo}\nEstoque anterior: {_produto.QuantidadeEstoque}",
                    Impact = "O registro ficara abaixo de zero ate que uma entrada ou ajuste regularize o saldo. Use apenas quando a operacao exigir override gerencial.",
                    Keyword = "NEGATIVO",
                    ConfirmButtonText = "Salvar mesmo assim"
                });
        }

        private static string ObterTextoComboBox(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Content?.ToString()?.Trim() ?? string.Empty;
            }

            return comboBox.Text?.Trim() ?? string.Empty;
        }

        private static void SelecionarComboBoxPorTexto(ComboBox comboBox, string valor)
        {
            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem &&
                    string.Equals(comboBoxItem.Content?.ToString(), valor, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            comboBox.Text = valor;
        }

        private static bool TryParseIntOrZero(string? text, out int value)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                value = 0;
                return true;
            }

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryParseDecimalOrZero(string? text, out decimal value)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                value = 0m;
                return true;
            }

            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static decimal CalcularMargem(decimal precoCompra, decimal precoVenda)
        {
            if (precoVenda <= 0)
            {
                return 0m;
            }

            return ((precoVenda - precoCompra) / precoVenda) * 100m;
        }

        private static string GerarCodigoBarras()
        {
            var seed = DateTime.Now.ToString("yyMMddHHmmss");
            return seed.PadRight(13, '0')[..13];
        }

        private struct ProdutoFormValues
        {
            public int Quantidade { get; set; }
            public int QuantidadeMinima { get; set; }
            public int QuantidadeMaxima { get; set; }
            public decimal PrecoCompra { get; set; }
            public decimal PrecoVenda { get; set; }
        }
    }
}
