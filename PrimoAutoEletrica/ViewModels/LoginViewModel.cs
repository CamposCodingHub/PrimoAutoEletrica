using Microsoft.Win32;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace PrimoAutoEletrica.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private readonly UserSessionService _userSessionService;

        private const string RegistryKey = @"SOFTWARE\PrimoAutoEletrica";

        public LoginViewModel(DatabaseService databaseService, LoggerService logger, UserSessionService userSessionService)
        {
            _databaseService = databaseService;
            _logger = logger;
            _userSessionService = userSessionService;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            set => SetField(ref _email, value);
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set => SetField(ref _password, value);
        }

        private bool _remember;
        public bool Remember
        {
            get => _remember;
            set => SetField(ref _remember, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetField(ref _errorMessage, value);
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set => SetField(ref _hasError, value);
        }

        public Funcionario? AuthenticatedFuncionario { get; private set; }

        public void LoadSavedCredentials()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKey);

                if (key == null)
                    return;

                var email = key.GetValue("SavedEmail") as string;
                var lembrar = key.GetValue("RememberMe") as string;

                if (string.IsNullOrWhiteSpace(email) || !string.Equals(lembrar, "true", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                Email = email;
                Password = string.Empty;
                Remember = true;
                RemoverSenhaLegadaDoRegistro(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao carregar credenciais salvas: {ex.Message}");
                Remember = false;
            }
        }

        private void RemoverSenhaLegadaDoRegistro(RegistryKey readOnlyKey)
        {
            if (readOnlyKey.GetValue("SavedPassword") == null)
            {
                return;
            }

            try
            {
                using RegistryKey writableKey = Registry.CurrentUser.CreateSubKey(RegistryKey);
                writableKey.DeleteValue("SavedPassword", false);
                _logger.LogInfo("Senha lembrada legada removida do registro local. O login passa a lembrar apenas o usuario.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao remover senha lembrada legada: {ex.Message}");
            }
        }

        private void SalvarCredenciais(bool lembrar)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKey);

                if (lembrar)
                {
                    key.SetValue("SavedEmail", Email);
                    key.SetValue("RememberMe", "true");
                    key.DeleteValue("SavedPassword", false);
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

        public bool Login()
        {
            try
            {
                HideError();

                string email = Email?.Trim() ?? string.Empty;
                string senha = Password ?? string.Empty;

                if (string.IsNullOrWhiteSpace(email))
                {
                    ShowError("Digite o e-mail.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(senha))
                {
                    ShowError("Digite a senha.");
                    return false;
                }

                var resultadoAutenticacao = _databaseService.AutenticarFuncionarioDetalhado(email, senha);
                AuthenticatedFuncionario = resultadoAutenticacao.Funcionario;

                if (resultadoAutenticacao.IsLocked)
                {
                    _logger.LogWarning($"Tentativa de login bloqueada para o usuario '{email}'.");
                    App.Audit.RegistrarLogin("LoginBloqueado", email, sucesso: false, resultadoAutenticacao.MensagemUsuario);
                    ShowError(resultadoAutenticacao.MensagemUsuario);
                    return false;
                }

                if (!resultadoAutenticacao.IsSuccess || AuthenticatedFuncionario == null)
                {
                    _logger.LogWarning($"Falha de autenticacao para o usuario '{email}'.");
                    App.Audit.RegistrarLogin("FalhaLogin", email, sucesso: false, resultadoAutenticacao.MensagemUsuario);
                    ShowError(resultadoAutenticacao.MensagemUsuario);
                    return false;
                }

                if (resultadoAutenticacao.RequiresPasswordChange)
                {
                    var trocaSenhaWindow = new TrocarSenhaObrigatoriaWindow(
                        AuthenticatedFuncionario,
                        senha,
                        _databaseService,
                        _logger)
                    {
                        Owner = Application.Current.MainWindow
                    };

                    if (trocaSenhaWindow.ShowDialog() != true)
                    {
                        ShowError("Troque a senha temporaria para continuar.");
                        return false;
                    }

                    senha = trocaSenhaWindow.NovaSenhaConfirmada;
                }

                SalvarCredenciais(Remember);

                App.Session.StartSession(AuthenticatedFuncionario);
                _userSessionService.CreateSession();
                App.Audit.RegistrarLogin("Login", AuthenticatedFuncionario.Email, sucesso: true, $"Perfil: {AuthenticatedFuncionario.PerfilAcesso}");

                _logger.LogInfo($"Login realizado com sucesso para '{AuthenticatedFuncionario.Email}' com perfil '{AuthenticatedFuncionario.PerfilAcesso}'.");

                return true;
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

                return false;
            }
        }

        public void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        public void HideError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
