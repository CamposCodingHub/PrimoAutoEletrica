using System.Windows;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Views
{
    public partial class LicenseActivationWindow : Window
    {
        private readonly string _appDataPath;
        private readonly LicenseService _licenseService;
        private readonly LoggerService? _logger;

        public LicenseActivationWindow(string appDataPath, LoggerService? logger = null)
        {
            InitializeComponent();
            _appDataPath = appDataPath;
            _logger = logger;
            _licenseService = new LicenseService(appDataPath, logger);

            HardwareIdText.Text = $"Hardware ID: {_licenseService.GenerateHardwareId()}";
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var licenseKey = LicenseKeyTextBox.Text.Trim();
                var companyName = CompanyNameTextBox.Text.Trim();
                var cnpj = CnpjTextBox.Text.Trim();

                if (string.IsNullOrEmpty(licenseKey))
                {
                    MessageBox.Show("Digite a chave de licença.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(companyName))
                {
                    MessageBox.Show("Digite o nome da empresa.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var success = _licenseService.ActivateLicense(licenseKey, companyName, cnpj);

                if (success)
                {
                    MessageBox.Show("Licença ativada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Erro ao ativar licença. Verifique os dados e tente novamente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                _logger?.LogError($"Erro ao ativar licença: {ex.Message}", ex);
                MessageBox.Show($"Erro ao ativar licença: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
