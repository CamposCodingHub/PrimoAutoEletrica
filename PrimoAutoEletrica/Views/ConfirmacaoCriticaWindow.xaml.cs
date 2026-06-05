using PrimoAutoEletrica.Services;
using System;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class ConfirmacaoCriticaWindow : Window
    {
        private readonly CriticalActionRequest _request;

        public ConfirmacaoCriticaWindow(CriticalActionRequest request)
        {
            InitializeComponent();
            _request = request ?? throw new ArgumentNullException(nameof(request));
            CarregarConteudo();
            Loaded += ConfirmacaoCriticaWindow_Loaded;
        }

        private void ConfirmacaoCriticaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ConfirmationTextBox.Focus();
            ConfirmationTextBox.SelectAll();
        }

        private void ConfirmationTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidationTextBlock.Visibility = Visibility.Collapsed;
            ConfirmarButton.IsEnabled = DigitacaoConfirmada();
        }

        private void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!DigitacaoConfirmada())
            {
                ValidationTextBlock.Text = $"Digite exatamente '{_request.Keyword}' para continuar.";
                ValidationTextBlock.Visibility = Visibility.Visible;
                ConfirmationTextBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CarregarConteudo()
        {
            Title = _request.WindowTitle;
            HeaderTextBlock.Text = _request.Header;
            SummaryTextBlock.Text = _request.Summary;
            DetailsTextBlock.Text = string.IsNullOrWhiteSpace(_request.Details)
                ? "Nenhum detalhe adicional foi informado."
                : _request.Details;
            ImpactTextBlock.Text = _request.Impact;
            KeywordTextBlock.Text = _request.Keyword;
            ConfirmarButton.Content = _request.ConfirmButtonText;
        }

        private bool DigitacaoConfirmada()
        {
            return string.Equals(
                ConfirmationTextBox.Text?.Trim(),
                _request.Keyword,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
