using PrimoAutoEletrica.Services;
using System;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    /// <summary>
    /// Janela de configuração de autenticação de dois fatores (2FA).
    /// Gera uma chave secreta TOTP e valida o código do usuário antes de ativar.
    /// </summary>
    public partial class TwoFactorSetupWindow : Window
    {
        private readonly ITwoFactorService _twoFactorService;
        private readonly string _userIdentifier;
        private string _secretKey = string.Empty;

        /// <summary>
        /// Chave secreta gerada. Disponível após verificação bem-sucedida (DialogResult = true).
        /// </summary>
        public string VerifiedSecretKey { get; private set; } = string.Empty;

        public TwoFactorSetupWindow(ITwoFactorService twoFactorService, string userIdentifier)
        {
            _twoFactorService = twoFactorService ?? throw new ArgumentNullException(nameof(twoFactorService));
            _userIdentifier = userIdentifier ?? throw new ArgumentNullException(nameof(userIdentifier));

            InitializeComponent();
            Loaded += TwoFactorSetupWindow_Loaded;
        }

        private void TwoFactorSetupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var (secretKey, otpAuthUrl) = _twoFactorService.GenerateSecret(_userIdentifier);
                _secretKey = secretKey;

                // Exibir chave formatada (grupos de 4)
                txtSecretKey.Text = FormatSecretKey(secretKey);
                txtOtpUrl.Text = otpAuthUrl;
                txtVerificationCode.Focus();
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Erro ao gerar chave: {ex.Message}";
                btnVerify.IsEnabled = false;
            }
        }

        private void BtnVerify_Click(object sender, RoutedEventArgs e)
        {
            var code = txtVerificationCode.Text?.Replace(" ", "").Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            {
                txtStatus.Text = "Digite o codigo de 6 digitos do seu aplicativo autenticador.";
                txtVerificationCode.Focus();
                return;
            }

            if (_twoFactorService.VerifyCode(_secretKey, code))
            {
                VerifiedSecretKey = _secretKey;

                App.Audit.RegistrarAcaoCritica("Seguranca", "2FA_Ativado", "Funcionario", _userIdentifier,
                    "Autenticacao de dois fatores ativada com sucesso.");

                MessageBox.Show(
                    "Autenticacao de dois fatores ativada com sucesso!\n\n" +
                    "A partir de agora, voce precisara informar o codigo do seu aplicativo autenticador " +
                    "ao fazer login.",
                    "2FA Ativado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            else
            {
                txtStatus.Text = "Codigo invalido. Verifique o codigo no seu aplicativo e tente novamente.";
                txtVerificationCode.Clear();
                txtVerificationCode.Focus();

                App.Audit.RegistrarLogin("2FA_FalhaVerificacao", _userIdentifier, false,
                    "Codigo 2FA invalido durante configuracao.");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCopyKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_secretKey);
                txtStatus.Foreground = (System.Windows.Media.Brush)FindResource("SuccessBrush");
                txtStatus.Text = "Chave copiada para a area de transferencia!";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"Falha ao copiar: {ex.Message}";
            }
        }

        private static string FormatSecretKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            var formatted = new System.Text.StringBuilder();
            for (int i = 0; i < key.Length; i++)
            {
                if (i > 0 && i % 4 == 0) formatted.Append(' ');
                formatted.Append(key[i]);
            }
            return formatted.ToString();
        }
    }
}
