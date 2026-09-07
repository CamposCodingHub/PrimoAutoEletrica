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

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSenhaConfirmacao.Password))
            {
                MessageBox.Show("Por favor, digite a senha de administrador.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Aqui entraria a validação real da senha de gerente do sistema.
            if (txtSenhaConfirmacao.Password != "admin123") // Placeholder de validação
            {
                MessageBox.Show("Senha incorreta!", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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

            var result = MessageBox.Show(
                "ATENÇÃO: Você está prestes a excluir dados permanentes. Isso apagará o histórico de Ordens de Serviço se Clientes/Produtos forem selecionados. Continuar?",
                "Confirmação Crítica",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    this.IsEnabled = false;
                    await _dbService.ZerarSistemaAsync(limparClientes, limparVeiculos, limparEstoque);
                    MessageBox.Show("Dados zerados com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao zerar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    this.IsEnabled = true;
                }
            }
        }
    }
}
