using PrimoAutoEletrica.Helpers;
using System;
using System.Globalization;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public sealed class OperacaoCaixaRequest
    {
        public string WindowTitle { get; init; } = "Operacao de caixa";
        public string Header { get; init; } = "Operacao de caixa";
        public string Subheader { get; init; } = string.Empty;
        public string ValorLabel { get; init; } = "Valor";
        public string ObservacoesLabel { get; init; } = "Observacoes";
        public string ConfirmButtonText { get; init; } = "Confirmar";
        public bool SolicitarValor { get; init; } = true;
        public bool PermitirZero { get; init; }
        public bool ObservacoesObrigatorias { get; init; }
        public decimal ValorInicial { get; init; }
        public string ObservacoesIniciais { get; init; } = string.Empty;
    }

    public partial class OperacaoCaixaWindow : Window
    {
        private readonly OperacaoCaixaRequest _request;

        public OperacaoCaixaWindow(OperacaoCaixaRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            InitializeComponent();
            Loaded += OperacaoCaixaWindow_Loaded;
        }

        public decimal ValorInformado { get; private set; }
        public string ObservacoesInformadas { get; private set; } = string.Empty;

        private void OperacaoCaixaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Title = _request.WindowTitle;
            HeaderTextBlock.Text = _request.Header;
            SubheaderTextBlock.Text = _request.Subheader;
            ValorLabelTextBlock.Text = _request.ValorLabel;
            ObservacoesLabelTextBlock.Text = _request.ObservacoesLabel;
            ConfirmarButton.Content = _request.ConfirmButtonText;
            ValorPanel.Visibility = _request.SolicitarValor ? Visibility.Visible : Visibility.Collapsed;
            ValorTextBox.Text = _request.ValorInicial > 0 ? _request.ValorInicial.ToString("F2", CultureInfo.CurrentCulture) : string.Empty;
            ObservacoesTextBox.Text = _request.ObservacoesIniciais;

            if (_request.SolicitarValor)
            {
                ValorTextBox.Focus();
                ValorTextBox.SelectAll();
            }
            else
            {
                ObservacoesTextBox.Focus();
            }
        }

        private void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_request.SolicitarValor)
            {
                if (!TryParseDecimal(ValorTextBox.Text, out var valor))
                {
                    MessageBox.Show("Informe um valor monetario valido.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_request.PermitirZero)
                {
                    ComercialValidationHelper.GarantirValorMaiorOuIgualZero(valor, "o valor informado");
                }
                else
                {
                    ComercialValidationHelper.GarantirValorMaiorQueZero(valor, "o valor informado");
                }

                ValorInformado = valor;
            }

            var observacoes = ObservacoesTextBox.Text?.Trim() ?? string.Empty;
            if (_request.ObservacoesObrigatorias && string.IsNullOrWhiteSpace(observacoes))
            {
                MessageBox.Show("Informe as observacoes antes de continuar.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ObservacoesInformadas = observacoes;
            try
            {
                DialogResult = true;
            }
            catch (InvalidOperationException)
            {
                Close();
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
            }
            catch (InvalidOperationException)
            {
                Close();
            }
        }

        private static bool TryParseDecimal(string? text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }
    }
}
