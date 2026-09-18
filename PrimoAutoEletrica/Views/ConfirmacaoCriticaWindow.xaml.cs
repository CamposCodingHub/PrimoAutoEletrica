using PrimoAutoEletrica.Services;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

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
            ContentRendered += ConfirmacaoCriticaWindow_ContentRendered;
            Activated += (_, _) => FocarCampoConfirmacao();
        }

        private void ConfirmacaoCriticaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            FocarCampoConfirmacao();
        }

        private void ConfirmacaoCriticaWindow_ContentRendered(object? sender, EventArgs e)
        {
            FocarCampoConfirmacao();
        }

        private void ConfirmationTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var confirmado = DigitacaoConfirmada();
            PlaceholderTextBlock.Visibility = string.IsNullOrEmpty(ConfirmationTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
            ValidationTextBlock.Visibility = Visibility.Collapsed;
            ConfirmarButton.IsEnabled = confirmado;
            ConfirmarButton.IsDefault = confirmado;
        }

        private void ConfirmarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!DigitacaoConfirmada())
            {
                ValidationTextBlock.Text = $"Digite exatamente '{_request.Keyword}' para continuar.";
                ValidationTextBlock.Visibility = Visibility.Visible;
                FocarCampoConfirmacao();
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
                ? string.Empty
                : _request.Details;
            DetailsTextBlock.Visibility = string.IsNullOrWhiteSpace(_request.Details)
                ? Visibility.Collapsed
                : Visibility.Visible;
            ImpactTextBlock.Text = _request.Impact;
            KeywordTextBlock.Text = _request.Keyword;
            PlaceholderTextBlock.Text = $"Clique aqui e digite {_request.Keyword}";
            ConfirmarButton.Content = _request.ConfirmButtonText;
            var destrutivo = _request.IsDestructive
                || string.Equals(_request.Keyword, "EXCLUIR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_request.Keyword, "CANCELAR", StringComparison.OrdinalIgnoreCase);
            ConfirmarButton.Style = FindResource(destrutivo
                ? "ConfirmDangerButton"
                : "ModalAccentButton") as Style ?? ConfirmarButton.Style;
        }

        private void FocarCampoConfirmacao()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsVisible || ConfirmationTextBox.IsKeyboardFocusWithin)
                {
                    return;
                }

                Activate();
                ConfirmationTextBox.IsReadOnly = false;
                ConfirmationTextBox.IsEnabled = true;
                ConfirmationTextBox.Focusable = true;
                ConfirmationTextBox.Focus();
                Keyboard.Focus(ConfirmationTextBox);
            }), DispatcherPriority.Input);
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
