using System;
using System.Windows;
using PrimoAutoEletrica.Models;
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

            CarregarInformacoesLicenca();
        }

        private void CarregarInformacoesLicenca()
        {
            var hwid = _licenseService.GenerateHardwareId();
            HwidDisplayTextBox.Text = hwid;

            var validacao = _licenseService.ValidateLicense();
            if (validacao.IsValid && validacao.License != null)
            {
                LicenseStatusText.Text = $"Status: Ativa ({validacao.License.Type}) - {validacao.License.CompanyName} (Expira em: {validacao.License.ExpirationDate:dd/MM/yyyy}, {validacao.License.RemainingDays} dias)";
            }
            else
            {
                LicenseStatusText.Text = $"Status: {validacao.Message}";
            }
        }

        private void CopyHwidButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(HwidDisplayTextBox.Text);
                MessageBox.Show("Hardware ID copiado para a área de transferência!", "Copiado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao copiar: {ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ActivateTokenButton_Click(object sender, RoutedEventArgs e)
        {
            var token = TokenActivationTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("Cole o token de ativação fornecido.", "Atenção", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = _licenseService.AtivarComToken(token);
            if (resultado.IsValid)
            {
                MessageBox.Show(resultado.Message, "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                CarregarInformacoesLicenca();
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(resultado.Message, "Falha na Ativação", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    CarregarInformacoesLicenca();
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Erro ao ativar licença. Verifique os dados e tente novamente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
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
