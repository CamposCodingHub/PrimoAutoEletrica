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
    public partial class NovoProdutoWindow : Window
    {
        private readonly PermissionService _permissionService;
        private string _selectedImagePath = string.Empty;
        private readonly List<string> _attachmentPaths = new();
        private readonly Produto? _produtoPrefill;

        public Produto? ProdutoCriado { get; private set; }

        public NovoProdutoWindow()
        {
            InitializeComponent();
            _permissionService = PermissionService.CriarParaSessaoAtual(App.Logger, App.Database);

            CategoriaComboBox.SelectedIndex = 0;
            UnidadeMedidaComboBox.SelectedIndex = 0;
            CodigoTextBox.Text = GerarCodigoInterno();
            CodigoAuxiliarTextBox.Text = "Novo cadastro";
            QuantidadeTextBox.Text = "0";
            QuantidadeMinimaTextBox.Text = "0";
            QuantidadeMaximaTextBox.Text = "0";
            PrecoCompraTextBox.Text = "0";
            PrecoVendaTextBox.Text = "0";

            AtualizarEstadoPerecivel();
            AtualizarIndicadoresFinanceiros();
            AtualizarAlertas();
            AtualizarListaAnexos();
        }

        public NovoProdutoWindow(Produto produtoPrefill)
            : this()
        {
            _produtoPrefill = produtoPrefill ?? throw new ArgumentNullException(nameof(produtoPrefill));
            Title = "Criar Produto a Partir do Catalogo";
            CodigoAuxiliarTextBox.Text = "Base do catalogo";
            PreencherFormulario(_produtoPrefill);
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
                    "Selecao de foto do produto validada em automacao sem abrir seletor de arquivos.",
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
                    "Selecione uma imagem valida (.jpg, .jpeg, .png ou .webp).",
                    "Imagem",
                    MessageBoxImage.Warning,
                    "Estoque");
                return;
            }

            _selectedImagePath = dialog.FileName;
            AtualizarPreviewFoto(_selectedImagePath);
        }

        private void RemoverFotoButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedImagePath = string.Empty;
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
                        "Anexo",
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
                WindowInteractionHelper.ShowMessage("Selecione um anexo para abrir.", "Anexo", MessageBoxImage.Information, "Estoque");
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
                WindowInteractionHelper.ShowMessage("O arquivo do anexo nao foi encontrado.", "Anexo", MessageBoxImage.Warning, "Estoque");
                return;
            }

            Process.Start(new ProcessStartInfo(resolvedPath) { UseShellExecute = true });
        }

        private void RemoverAnexoButton_Click(object sender, RoutedEventArgs e)
        {
            if (AnexosListBox.SelectedItem is not string path)
            {
                WindowInteractionHelper.ShowMessage("Selecione um anexo para remover.", "Anexo", MessageBoxImage.Information, "Estoque");
                return;
            }

            _attachmentPaths.RemoveAll(candidate => string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase));
            AtualizarListaAnexos();
        }

        private void GerarCodigoButton_Click(object sender, RoutedEventArgs e)
        {
            CodigoBarrasTextBox.Text = GerarCodigoBarras();

            if (string.IsNullOrWhiteSpace(CodigoTextBox.Text))
            {
                CodigoTextBox.Text = GerarCodigoInterno();
            }
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
                if (!ValidarPermissao("ESTOQUE_CRIAR", "Voce nao possui permissao para cadastrar produtos no estoque."))
                {
                    return;
                }

                if (!TryReadFormValues(out var formValues))
                {
                    return;
                }

                var produtoId = Guid.NewGuid();
                var codigoInterno = string.IsNullOrWhiteSpace(CodigoTextBox.Text)
                    ? GerarCodigoInterno()
                    : CodigoTextBox.Text.Trim();

                var produto = new Produto
                {
                    Id = produtoId,
                    Codigo = codigoInterno,
                    Nome = NomeTextBox.Text.Trim(),
                    Descricao = DescricaoTextBox.Text.Trim(),
                    Categoria = ObterTextoComboBox(CategoriaComboBox),
                    Marca = MarcaTextBox.Text.Trim(),
                    Modelo = ModeloTextBox.Text.Trim(),
                    Fornecedor = FornecedorTextBox.Text.Trim(),
                    CNPJFornecedor = CnpjFornecedorTextBox.Text.Trim(),
                    ContatoFornecedor = ContatoFornecedorTextBox.Text.Trim(),
                    TelefoneFornecedor = TelefoneFornecedorTextBox.Text.Trim(),
                    QuantidadeEstoque = formValues.Quantidade,
                    QuantidadeMinima = formValues.QuantidadeMinima,
                    QuantidadeMaxima = formValues.QuantidadeMaxima,
                    Localizacao = LocalizacaoTextBox.Text.Trim(),
                    Prateleira = PrateleiraTextBox.Text.Trim(),
                    Gaveta = GavetaTextBox.Text.Trim(),
                    PrecoCompra = formValues.PrecoCompra,
                    PrecoVenda = formValues.PrecoVenda,
                    MargemLucro = CalcularMargem(formValues.PrecoCompra, formValues.PrecoVenda),
                    ValorTotalEstoque = formValues.Quantidade * formValues.PrecoCompra,
                    UnidadeMedida = ObterTextoComboBox(UnidadeMedidaComboBox),
                    Peso = PesoTextBox.Text.Trim(),
                    Dimensoes = DimensoesTextBox.Text.Trim(),
                    Cor = CorTextBox.Text.Trim(),
                    Material = MaterialTextBox.Text.Trim(),
                    CodigoBarras = CodigoBarrasTextBox.Text.Trim(),
                    SKU = SkuTextBox.Text.Trim(),
                    NCMS = NcmsTextBox.Text.Trim(),
                    CEST = CestTextBox.Text.Trim(),
                    CFOP = CfopTextBox.Text.Trim(),
                    Ativo = AtivoCheckBox.IsChecked == true,
                    ProdutoPerecivel = ProdutoPerecivelCheckBox.IsChecked == true,
                    DataValidade = DataValidadeDatePicker.SelectedDate,
                    DataFabricacao = DataFabricacaoDatePicker.SelectedDate,
                    Lote = LoteTextBox.Text.Trim(),
                    DataCadastro = DateTime.Now,
                    DataUltimaAtualizacao = DateTime.Now,
                    Observacoes = ObservacoesTextBox.Text.Trim(),
                    ImagemUrl = SalvarImagemSelecionada(produtoId),
                    Anexos = ProdutoMediaService.PersistSelectedAttachments(_attachmentPaths, produtoId, NomeTextBox.Text.Trim())
                };

                App.Repositories.Produtos.Inserir(produto);
                ProdutoCriado = produto;

                WindowInteractionHelper.ShowMessage(
                    "Produto cadastrado com sucesso!",
                    "Sucesso",
                    MessageBoxImage.Information,
                    "Estoque");

                WindowInteractionHelper.CloseWithDialogResult(this, true, "Estoque");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(
                    $"Erro ao salvar produto:\n{ex.Message}",
                    "Erro",
                    MessageBoxImage.Error,
                    "Estoque",
                    ex);
            }
        }

        private string SalvarImagemSelecionada(Guid produtoId)
        {
            if (string.IsNullOrWhiteSpace(_selectedImagePath))
            {
                return string.Empty;
            }

            return ProdutoMediaService.PersistSelectedImage(_selectedImagePath, produtoId, NomeTextBox.Text.Trim());
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
            CodigoAuxiliarTextBox.Text = string.IsNullOrWhiteSpace(CodigoTextBox.Text)
                ? "Gerado automaticamente"
                : CodigoTextBox.Text.Trim();

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
                WindowInteractionHelper.ShowMessage(
                    "O nome do produto e obrigatorio.",
                    "Validacao",
                    MessageBoxImage.Warning,
                    "Estoque");
                NomeTextBox.Focus();
                return false;
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
                "Validacao",
                MessageBoxImage.Warning,
                "Estoque");
            control.Focus();
            return false;
        }

        private bool ValidarPermissao(string codigoPermissao, string mensagem)
        {
            if (_permissionService.TemPermissaoCodigo(codigoPermissao))
            {
                return true;
            }

            WindowInteractionHelper.ShowMessage(mensagem, "Acesso negado", MessageBoxImage.Warning, "Estoque");
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

            return CriticalActionDialogService.ConfirmarAcao(
                this,
                new CriticalActionRequest
                {
                    WindowTitle = "Confirmar estoque negativo",
                    Header = "Criacao com estoque negativo",
                    Summary = $"Voce esta prestes a criar o produto com estoque negativo ({quantidade}).",
                    Details = $"Produto: {NomeTextBox.Text.Trim()}\nCodigo: {CodigoTextBox.Text.Trim()}\nCategoria: {ObterTextoComboBox(CategoriaComboBox)}",
                    Impact = "O cadastro sera salvo abaixo de zero e exigira acompanhamento operacional imediato para evitar vendas e ajustes incorretos.",
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

        private static string GerarCodigoInterno()
        {
            return $"PRD-{DateTime.Now:yyMMddHHmmss}";
        }

        private static string GerarCodigoBarras()
        {
            var seed = DateTime.Now.ToString("yyMMddHHmmss");
            return seed.PadRight(13, '0')[..13];
        }

        private void PreencherFormulario(Produto produto)
        {
            CodigoTextBox.Text = string.IsNullOrWhiteSpace(produto.Codigo) ? CodigoTextBox.Text : produto.Codigo;
            NomeTextBox.Text = produto.Nome ?? string.Empty;
            DefinirValorCombo(CategoriaComboBox, produto.Categoria);
            DefinirValorCombo(UnidadeMedidaComboBox, string.IsNullOrWhiteSpace(produto.UnidadeMedida) ? "UN" : produto.UnidadeMedida);
            MarcaTextBox.Text = produto.Marca ?? string.Empty;
            ModeloTextBox.Text = produto.Modelo ?? string.Empty;
            DescricaoTextBox.Text = produto.Descricao ?? string.Empty;
            CodigoBarrasTextBox.Text = produto.CodigoBarras ?? string.Empty;
            SkuTextBox.Text = produto.SKU ?? string.Empty;
            CorTextBox.Text = produto.Cor ?? string.Empty;
            MaterialTextBox.Text = produto.Material ?? string.Empty;
            PesoTextBox.Text = produto.Peso ?? string.Empty;
            DimensoesTextBox.Text = produto.Dimensoes ?? string.Empty;
            AtivoCheckBox.IsChecked = produto.Ativo;
            QuantidadeTextBox.Text = produto.QuantidadeEstoque.ToString(CultureInfo.InvariantCulture);
            QuantidadeMinimaTextBox.Text = produto.QuantidadeMinima.ToString(CultureInfo.InvariantCulture);
            QuantidadeMaximaTextBox.Text = produto.QuantidadeMaxima.ToString(CultureInfo.InvariantCulture);
            LocalizacaoTextBox.Text = produto.Localizacao ?? string.Empty;
            PrateleiraTextBox.Text = produto.Prateleira ?? string.Empty;
            GavetaTextBox.Text = produto.Gaveta ?? string.Empty;
            PrecoCompraTextBox.Text = produto.PrecoCompra.ToString(CultureInfo.InvariantCulture);
            PrecoVendaTextBox.Text = produto.PrecoVenda.ToString(CultureInfo.InvariantCulture);
            NcmsTextBox.Text = produto.NCMS ?? string.Empty;
            CestTextBox.Text = produto.CEST ?? string.Empty;
            CfopTextBox.Text = produto.CFOP ?? string.Empty;
            FornecedorTextBox.Text = produto.Fornecedor ?? string.Empty;
            CnpjFornecedorTextBox.Text = produto.CNPJFornecedor ?? string.Empty;
            ContatoFornecedorTextBox.Text = produto.ContatoFornecedor ?? string.Empty;
            TelefoneFornecedorTextBox.Text = produto.TelefoneFornecedor ?? string.Empty;
            ProdutoPerecivelCheckBox.IsChecked = produto.ProdutoPerecivel;
            DataFabricacaoDatePicker.SelectedDate = produto.DataFabricacao;
            DataValidadeDatePicker.SelectedDate = produto.DataValidade;
            LoteTextBox.Text = produto.Lote ?? string.Empty;
            ObservacoesTextBox.Text = produto.Observacoes ?? string.Empty;
            CodigoAuxiliarTextBox.Text = string.IsNullOrWhiteSpace(produto.Codigo) ? "Base do catalogo" : produto.Codigo;

            if (!string.IsNullOrWhiteSpace(produto.ImagemUrl))
            {
                var imagePath = ProdutoMediaService.ResolveExistingPath(produto.ImagemUrl);
                if (!string.IsNullOrWhiteSpace(imagePath))
                {
                    _selectedImagePath = imagePath;
                    AtualizarPreviewFoto(imagePath);
                }
            }

            AtualizarEstadoPerecivel();
            AtualizarIndicadoresFinanceiros();
            AtualizarAlertas();
        }

        private static void DefinirValorCombo(ComboBox comboBox, string? value)
        {
            if (comboBox == null)
            {
                return;
            }

            var texto = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(texto))
            {
                return;
            }

            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem &&
                    string.Equals(comboBoxItem.Content?.ToString()?.Trim(), texto, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }

            comboBox.Text = texto;
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
