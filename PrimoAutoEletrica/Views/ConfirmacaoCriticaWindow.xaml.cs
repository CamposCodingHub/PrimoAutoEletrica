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
        }

        private void ConfirmacaoCriticaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PrepararCampoConfirmacao();
        }

        private void ConfirmacaoCriticaWindow_ContentRendered(object? sender, EventArgs e)
        {
            FocarCampoConfirmacao();
        }

        private void ConfirmationTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!ConfirmationTextBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                FocarCampoConfirmacao();
            }
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
                ? "Nenhum detalhe adicional foi informado."
                : _request.Details;
            ImpactTextBlock.Text = _request.Impact;
            KeywordHintTextBlock.Text = $"Palavra obrigatoria: {_request.Keyword}";
            ConfirmationTextBox.Tag = _request.Keyword;
            ConfirmarButton.Content = _request.ConfirmButtonText;
            var destrutivo = _request.IsDestructive
                || string.Equals(_request.Keyword, "EXCLUIR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_request.Keyword, "CANCELAR", StringComparison.OrdinalIgnoreCase);
            ConfirmarButton.Style = FindResource(destrutivo
                ? "ConfirmDangerButton"
                : "ModalAccentButton") as Style ?? ConfirmarButton.Style;
        }

        private void PrepararCampoConfirmacao()
        {
            ConfirmationTextBox.IsReadOnly = false;
            ConfirmationTextBox.IsEnabled = true;
            ConfirmationTextBox.Focusable = true;
            ConfirmationTextBox.IsHitTestVisible = true;
            ConfirmationTextBox.IsTabStop = true;
            ConfirmationTextBox.Clear();
            FocarCampoConfirmacao();
        }

        private void FocarCampoConfirmacao()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Activate();
                ConfirmationTextBox.Focus();
                Keyboard.Focus(ConfirmationTextBox);
                ConfirmationTextBox.CaretIndex = ConfirmationTextBox.Text?.Length ?? 0;
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
