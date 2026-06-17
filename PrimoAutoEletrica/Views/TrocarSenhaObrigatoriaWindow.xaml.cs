using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace PrimoAutoEletrica.Views
{
    public partial class TrocarSenhaObrigatoriaWindow : Window
    {
        private readonly Funcionario _funcionario;
        private readonly string _senhaAtual;
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;

        public string NovaSenhaConfirmada { get; private set; } = string.Empty;

        public TrocarSenhaObrigatoriaWindow(
            Funcionario funcionario,
            string senhaAtual,
            DatabaseService databaseService,
            LoggerService logger)
        {
            InitializeComponent();

            _funcionario = funcionario ?? throw new ArgumentNullException(nameof(funcionario));
            _senhaAtual = senhaAtual ?? string.Empty;
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            UsuarioTextBlock.Text = $"Usuario: {_funcionario.Nome} ({_funcionario.Email}). Esta senha temporaria precisa ser substituida para continuar.";
            Loaded += (_, _) => NovaSenhaPasswordBox.Focus();
            KeyDown += TrocarSenhaObrigatoriaWindow_KeyDown;
        }

        private void TrocarSenhaObrigatoriaWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SalvarButton_Click(sender, e);
            }
        }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                HideError();

                var novaSenha = NovaSenhaPasswordBox.Password;
                var confirmarSenha = ConfirmarSenhaPasswordBox.Password;

                var erro = ValidarSenha(novaSenha, confirmarSenha);
                if (!string.IsNullOrWhiteSpace(erro))
                {
                    ShowError(erro);
                    return;
                }

                _databaseService.AlterarSenhaFuncionario(
                    _funcionario.Id,
                    novaSenha,
                    exigirTrocaSenha: false,
                    operador: _funcionario.Email);

                _funcionario.ExigirTrocaSenha = false;
                NovaSenhaConfirmada = novaSenha;

                App.Audit.RegistrarLogin("TrocaSenhaObrigatoriaConcluida", _funcionario.Email, sucesso: true);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao trocar senha obrigatoria.", ex);
                ShowError("Nao foi possivel salvar a nova senha. Consulte os logs e tente novamente.");
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            App.Audit.RegistrarLogin("TrocaSenhaObrigatoriaCancelada", _funcionario.Email, sucesso: false);
            DialogResult = false;
            Close();
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

            if (!novaSenha.Any(char.IsLetter) || !novaSenha.Any(char.IsDigit))
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

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorBorder.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorTextBlock.Text = string.Empty;
            ErrorBorder.Visibility = Visibility.Collapsed;
        }
    }
}
