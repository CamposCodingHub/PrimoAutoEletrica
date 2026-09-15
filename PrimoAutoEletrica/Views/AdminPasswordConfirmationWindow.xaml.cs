using PrimoAutoEletrica.Services;
using System;
using System.Windows;
using PrimoAutoEletrica.Helpers;

namespace PrimoAutoEletrica.Views
{
    public partial class AdminPasswordConfirmationWindow : Window
    {
        private readonly DatabaseService _dbService;
        public bool IsConfirmed { get; private set; } = false;

        public AdminPasswordConfirmationWindow(string title = "Confirmação de Administrador", string message = "Esta ação requer confirmação de senha de administrador.")
        {
            InitializeComponent();
            _dbService = App.Database;
            
            TitleTextBlock.Text = title;
            MessageTextBlock.Text = message;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSenhaConfirmacao.Password))
            {
                MessageBox.Show("Por favor, digite a senha de administrador.", UiText.T("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var emailAdmin = App.Session?.UserEmail;
            if (string.IsNullOrWhiteSpace(emailAdmin))
            {
                MessageBox.Show("Sessão inválida. Faça login novamente como administrador.", UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var auth = _dbService.AutenticarFuncionarioDetalhado(emailAdmin, txtSenhaConfirmacao.Password);
            if (!auth.IsSuccess || auth.Funcionario == null)
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(auth.MensagemUsuario) ? "Senha incorreta!" : auth.MensagemUsuario,
                    UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var perfil = auth.Funcionario.PerfilAcesso?.Trim() ?? "";
            if (!perfil.Equals("Administrador", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Apenas Administradores podem realizar esta ação.", UiText.T("AccessDenied"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsConfirmed = true;
            DialogResult = true;
            Close();
        }
    }
}
