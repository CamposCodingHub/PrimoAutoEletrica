using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public partial class NovaFerramentaDialog : Window
    {
        private readonly IToolService _toolService;
        public Tool? FerramentaCriada { get; private set; }

        public NovaFerramentaDialog(IToolService? toolService = null)
        {
            InitializeComponent();
            _toolService = toolService ?? new ToolService();
        }

        private async void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;

            var codigo = CodigoTextBox.Text?.Trim();
            var nome = NomeTextBox.Text?.Trim();
            var categoriaItem = CategoriaComboBox.SelectedItem as ComboBoxItem;
            var categoria = categoriaItem?.Content?.ToString() ?? "Geral";
            var localizacao = LocalizacaoTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length < 3)
            {
                MostrarErro("Informe um código válido com pelo menos 3 caracteres (Ex: FER-010).");
                CodigoTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(nome))
            {
                MostrarErro("Informe o nome ou identificação da ferramenta.");
                NomeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(localizacao))
            {
                MostrarErro("Informe a localização física da ferramenta na oficina.");
                LocalizacaoTextBox.Focus();
                return;
            }

            decimal valor = 0m;
            if (!string.IsNullOrWhiteSpace(ValorTextBox.Text))
            {
                var valorLimpo = ValorTextBox.Text.Replace("R$", "").Trim();
                if (!decimal.TryParse(valorLimpo, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out valor) &&
                    !decimal.TryParse(valorLimpo, NumberStyles.Any, CultureInfo.InvariantCulture, out valor))
                {
                    MostrarErro("Valor de compra inválido. Utilize formato 0,00.");
                    ValorTextBox.Focus();
                    return;
                }
            }

            var nova = new Tool
            {
                ToolId = Guid.NewGuid(),
                Code = codigo.ToUpperInvariant(),
                Name = nome,
                Category = categoria,
                Brand = string.IsNullOrWhiteSpace(MarcaTextBox.Text) ? null : MarcaTextBox.Text.Trim(),
                Model = string.IsNullOrWhiteSpace(ModeloTextBox.Text) ? null : ModeloTextBox.Text.Trim(),
                SerialNumber = string.IsNullOrWhiteSpace(SerialTextBox.Text) ? null : SerialTextBox.Text.Trim(),
                PatrimonyNumber = string.IsNullOrWhiteSpace(PatrimonioTextBox.Text) ? null : PatrimonioTextBox.Text.Trim(),
                LocationName = localizacao,
                PurchaseDate = DateTime.Today,
                PurchaseValue = valor,
                Notes = string.IsNullOrWhiteSpace(ObservacoesTextBox.Text) ? null : ObservacoesTextBox.Text.Trim(),
                Status = ToolStatus.AVAILABLE,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            try
            {
                SalvarButton.IsEnabled = false;
                var ok = await _toolService.SalvarFerramentaAsync(nova);
                if (ok)
                {
                    FerramentaCriada = nova;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MostrarErro("Não foi possível salvar a ferramenta. Verifique se o código já existe.");
                    SalvarButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MostrarErro(ex.Message);
                SalvarButton.IsEnabled = true;
            }
        }

        private void MostrarErro(string mensagem)
        {
            ValidationMessageTextBlock.Text = mensagem;
            ValidationMessageTextBlock.Visibility = Visibility.Visible;
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
