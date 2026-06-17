using PrimoAutoEletrica.Services;
using System;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class FriendlyErrorWindow : Window
    {
        private readonly FriendlyErrorInfo _errorInfo;

        public FriendlyErrorWindow(FriendlyErrorInfo errorInfo)
        {
            InitializeComponent();
            _errorInfo = errorInfo ?? throw new ArgumentNullException(nameof(errorInfo));

            SubtitleTextBlock.Text = $"Codigo do erro: {_errorInfo.CorrelationId}";
            UserMessageTextBlock.Text = _errorInfo.UserMessage;
            TechnicalDetailsTextBox.Text = _errorInfo.TechnicalDetails;
        }

        private void CopyErrorButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_errorInfo.TechnicalDetails);
            CopyErrorButton.Content = "Erro copiado";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
