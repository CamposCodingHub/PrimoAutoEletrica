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
        private readonly ViewModels.TrocarSenhaObrigatoriaViewModel _viewModel;

        public string NovaSenhaConfirmada => _viewModel?.NovaSenhaConfirmada ?? string.Empty;

        public TrocarSenhaObrigatoriaWindow(
            Funcionario funcionario,
            string senhaAtual,
            DatabaseService databaseService,
            LoggerService logger)
        {
            InitializeComponent();

            _viewModel = new ViewModels.TrocarSenhaObrigatoriaViewModel(funcionario, senhaAtual, databaseService, logger);
            DataContext = _viewModel;

            UsuarioTextBlock.Text = $"Usuario: {funcionario.Nome} ({funcionario.Email}). Esta senha temporaria precisa ser substituida para continuar.";
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

                _viewModel.NovaSenha = NovaSenhaPasswordBox.Password;
                _viewModel.ConfirmarSenha = ConfirmarSenhaPasswordBox.Password;

                if (!_viewModel.Save())
                {
                    ShowError(_viewModel.ErrorMessage);
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Falha ao trocar senha obrigatoria.", ex);
                ShowError("Nao foi possivel salvar a nova senha. Consulte os logs e tente novamente.");
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Cancel();
            DialogResult = false;
            Close();
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
