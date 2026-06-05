using Microsoft.Win32;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimoAutoEletrica.Views
{
    public partial class LoginWindow : Window
    {
        private readonly DatabaseService _databaseService = null!;
        private readonly LoggerService _logger;
        private readonly UserSessionService _userSessionService;

        private Funcionario? _funcionarioLogado;
        private bool _mostrarSenha;

        private const string RegistryKey = @"SOFTWARE\PrimoAutoEletrica";

        public LoginWindow()
        {
            InitializeComponent();

            _databaseService = global::PrimoAutoEletrica.App.Database;
            _logger = App.Logger;
            _userSessionService = new UserSessionService(_databaseService, _logger, global::PrimoAutoEletrica.App.Session);

            Loaded += LoginWindow_Loaded;

            EmailTextBox.KeyDown += TextBox_KeyDown;
            SenhaPasswordBox.KeyDown += TextBox_KeyDown;
            SenhaTextBox.KeyDown += TextBox_KeyDown;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarCredenciaisSalvas();

            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                SenhaPasswordBox.Focus();
                return;
            }

            EmailTextBox.Focus();
        }

        private void MostrarSenhaButton_Click(object sender, RoutedEventArgs e)
        {
            _mostrarSenha = !_mostrarSenha;

            if (_mostrarSenha)
            {
                SenhaTextBox.Text = SenhaPasswordBox.Password;
                SenhaPasswordBox.Visibility = Visibility.Collapsed;
                SenhaTextBox.Visibility = Visibility.Visible;
                ((TextBlock)MostrarSenhaButton.Content).Text = "Ocultar";
                return;
            }

            SenhaPasswordBox.Password = SenhaTextBox.Text;
            SenhaPasswordBox.Visibility = Visibility.Visible;
            SenhaTextBox.Visibility = Visibility.Collapsed;
            ((TextBlock)MostrarSenhaButton.Content).Text = "Mostrar";
        }

        private void SenhaPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_mostrarSenha)
            {
                SenhaTextBox.Text = SenhaPasswordBox.Password;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HideError();

                string email = EmailTextBox.Text.Trim();
                string senha = _mostrarSenha ? SenhaTextBox.Text : SenhaPasswordBox.Password;

                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowError("Digite o e-mail.");
                    EmailTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(senha))
                {
                    ShowError("Digite a senha.");

                    if (_mostrarSenha)
                    {
                        SenhaTextBox.Focus();
                    }
                    else
                    {
                        SenhaPasswordBox.Focus();
                    }

                    return;
                }

                var resultadoAutenticacao = _databaseService.AutenticarFuncionarioDetalhado(email, senha);
                _funcionarioLogado = resultadoAutenticacao.Funcionario;

                if (resultadoAutenticacao.IsLocked)
                {
                    _logger.LogWarning($"Tentativa de login bloqueada para o usuario '{email}'.");
                    App.Audit.RegistrarLogin("LoginBloqueado", email, sucesso: false, resultadoAutenticacao.MensagemUsuario);
                    ShowError(resultadoAutenticacao.MensagemUsuario);
                    return;
                }

                if (!resultadoAutenticacao.IsSuccess || _funcionarioLogado == null)
                {
                    _logger.LogWarning($"Falha de autenticacao para o usuario '{email}'.");
                    App.Audit.RegistrarLogin("FalhaLogin", email, sucesso: false, resultadoAutenticacao.MensagemUsuario);
                    ShowError(resultadoAutenticacao.MensagemUsuario);
                    return;
                }

                SalvarCredenciais(LembrarCheckBox.IsChecked == true);

                global::PrimoAutoEletrica.App.Session.StartSession(_funcionarioLogado);
                _userSessionService.CreateSession();
                App.Audit.RegistrarLogin("Login", _funcionarioLogado.Email, sucesso: true, $"Perfil: {_funcionarioLogado.PerfilAcesso}");

                var mainWindow = new MainWindow(_funcionarioLogado);
                Application.Current.MainWindow = mainWindow;

                _logger.LogInfo($"Login realizado com sucesso para '{_funcionarioLogado.Email}' com perfil '{_funcionarioLogado.PerfilAcesso}'.");

                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro inesperado ao processar login.", ex);

                MessageBox.Show(
                    "Nao foi possivel concluir o login. Consulte os logs para mais detalhes.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void CarregarCredenciaisSalvas()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKey);

                if (key == null)
                {
                    return;
                }

                var email = key.GetValue("SavedEmail") as string;
                var senhaProtegida = key.GetValue("SavedPassword") as string;
                var lembrar = key.GetValue("RememberMe") as string;

                if (string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(senhaProtegida) ||
                    !string.Equals(lembrar, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                EmailTextBox.Text = email;
                SenhaPasswordBox.Password = DesprotegerTexto(senhaProtegida);
                LembrarCheckBox.IsChecked = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao carregar credenciais salvas: {ex.Message}");
                LembrarCheckBox.IsChecked = false;
            }
        }

        private void SalvarCredenciais(bool lembrar)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKey);

                if (lembrar)
                {
                    key.SetValue("SavedEmail", EmailTextBox.Text);
                    key.SetValue("SavedPassword", ProtegerTexto(_mostrarSenha ? SenhaTextBox.Text : SenhaPasswordBox.Password));
                    key.SetValue("RememberMe", "true");
                    return;
                }

                key.DeleteValue("SavedEmail", false);
                key.DeleteValue("SavedPassword", false);
                key.DeleteValue("RememberMe", false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao salvar credenciais locais: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorMessageTextBlock.Text = string.Empty;
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
        }

        private void EsqueciSenhaButton_Click(object sender, RoutedEventArgs e)
        {
            App.Audit.RegistrarAcaoCritica("Seguranca", "SolicitarRecuperacaoSenha", "Login", EmailTextBox.Text.Trim());

            MessageBox.Show(
                "Solicite a redefinicao de senha a um administrador do sistema. A tentativa foi registrada para auditoria.",
                "Recuperacao de Senha",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInfo("Encerramento solicitado na tela de login.");
            App.Audit.RegistrarSistema("EncerramentoLogin", "Encerramento solicitado na tela de login.");
            _userSessionService.EndSession();
            global::PrimoAutoEletrica.App.Session.EndSession();
            Application.Current.Shutdown();
        }

        private static string ProtegerTexto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(valor);
            var protegidos = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protegidos);
        }

        private static string DesprotegerTexto(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            try
            {
                var protegidos = Convert.FromBase64String(valor);
                var bytes = ProtectedData.Unprotect(protegidos, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                return valor;
            }
            catch (CryptographicException)
            {
                return valor;
            }
        }
    }
}

