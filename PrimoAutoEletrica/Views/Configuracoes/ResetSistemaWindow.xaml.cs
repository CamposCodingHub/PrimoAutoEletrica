using PrimoAutoEletrica.Services;
using System;
using System.Windows;

namespace PrimoAutoEletrica.Views.Configuracoes
{
    public partial class ResetSistemaWindow : Window
    {
        private readonly DatabaseService _dbService;

        public ResetSistemaWindow(DatabaseService dbService)
        {
            InitializeComponent();
            _dbService = dbService;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();

        private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSenhaConfirmacao.Password))
            {
                MessageBox.Show("Por favor, digite a senha de administrador.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var emailAdmin = App.Session?.UserEmail;
            if (string.IsNullOrWhiteSpace(emailAdmin))
            {
                MessageBox.Show("Sessao invalida. Faca login novamente como administrador.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var auth = _dbService.AutenticarFuncionarioDetalhado(emailAdmin, txtSenhaConfirmacao.Password);
            if (!auth.IsSuccess || auth.Funcionario == null)
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(auth.MensagemUsuario) ? "Senha incorreta!" : auth.MensagemUsuario,
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var perfil = auth.Funcionario.PerfilAcesso?.Trim() ?? "";
            if (!perfil.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
                !perfil.Equals("Gerente", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Apenas Administrador ou Gerente podem zerar o sistema.", "Acesso negado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool limparClientes = chkClientes.IsChecked == true;
            bool limparVeiculos = chkVeiculos.IsChecked == true;
            bool limparEstoque = chkEstoque.IsChecked == true;
            if (!limparClientes && !limparVeiculos && !limparEstoque)
            {
                MessageBox.Show("Selecione pelo menos uma opção para zerar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                    "ATENÇÃO: exclusao permanente de dados. Continuar?",
                    "Confirmação Crítica", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
                return;

            try
            {
                IsEnabled = false;
                await _dbService.ZerarSistemaAsync(limparClientes, limparVeiculos, limparEstoque);
                MessageBox.Show("Dados zerados com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao zerar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsEnabled = true; }
        }
    }
}
