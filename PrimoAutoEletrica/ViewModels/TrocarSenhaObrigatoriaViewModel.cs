using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PrimoAutoEletrica.ViewModels
{
    public class TrocarSenhaObrigatoriaViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private readonly Funcionario _funcionario;
        private readonly string _senhaAtual;

        public TrocarSenhaObrigatoriaViewModel(Funcionario funcionario, string senhaAtual, DatabaseService databaseService, LoggerService logger)
        {
            _funcionario = funcionario ?? throw new ArgumentNullException(nameof(funcionario));
            _senhaAtual = senhaAtual ?? string.Empty;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string _novaSenha = string.Empty;
        public string NovaSenha { get => _novaSenha; set => SetField(ref _novaSenha, value); }

        private string _confirmarSenha = string.Empty;
        public string ConfirmarSenha { get => _confirmarSenha; set => SetField(ref _confirmarSenha, value); }

        private string _errorMessage = string.Empty;
        public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

        private bool _hasError;
        public bool HasError { get => _hasError; set => SetField(ref _hasError, value); }

        public string NovaSenhaConfirmada { get; private set; } = string.Empty;

        public bool Save()
        {
            try
            {
                HideError();

                var erro = ValidarSenha(NovaSenha, ConfirmarSenha);
                if (!string.IsNullOrWhiteSpace(erro))
                {
                    ShowError(erro);
                    return false;
                }

                _databaseService.AlterarSenhaFuncionario(_funcionario.Id, NovaSenha, exigirTrocaSenha: false, operador: _funcionario.Email);
                _funcionario.ExigirTrocaSenha = false;
                NovaSenhaConfirmada = NovaSenha;

                App.Audit.RegistrarLogin("TrocaSenhaObrigatoriaConcluida", _funcionario.Email, sucesso: true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao trocar senha obrigatoria.", ex);
                ShowError("Nao foi possivel salvar a nova senha. Consulte os logs e tente novamente.");
                return false;
            }
        }

        public void Cancel()
        {
            App.Audit.RegistrarLogin("TrocaSenhaObrigatoriaCancelada", _funcionario.Email, sucesso: false);
        }

        private string ValidarSenha(string novaSenha, string confirmarSenha)
        {
            if (string.IsNullOrWhiteSpace(novaSenha))
            {
                return "Digite a nova senha.";
            }

            if (novaSenha.Length < 8)
            {
                return "A nova senha deve ter pelo menos 8 caracteres.";
            }

            if (!ContainsLetter(novaSenha) || !ContainsDigit(novaSenha))
            {
                return "A nova senha deve combinar letras e numeros.";
            }

            if (string.Equals(novaSenha, _senhaAtual, StringComparison.Ordinal))
            {
                return "A nova senha nao pode ser igual a senha temporaria.";
            }

            if (!string.Equals(novaSenha, confirmarSenha, StringComparison.Ordinal))
            {
                return "A confirmacao da senha nao confere.";
            }

            return string.Empty;
        }

        private static bool ContainsLetter(string s) => System.Linq.Enumerable.Any(s, char.IsLetter);
        private static bool ContainsDigit(string s) => System.Linq.Enumerable.Any(s, char.IsDigit);

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
