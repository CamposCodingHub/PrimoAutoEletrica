using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.Windows;

namespace PrimoAutoEletrica.Views
{
    public partial class UsuariosOnlineWindow : Window
    {
        private readonly UserSessionService _userSessionService;
        private readonly LoggerService _logger;

        public UsuariosOnlineWindow()
        {
            InitializeComponent();

            _userSessionService = new UserSessionService(App.Database, App.Logger, App.Session);
            _logger = App.Logger;

            Loaded += UsuariosOnlineWindow_Loaded;
        }

        private void UsuariosOnlineWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarUsuariosOnline();
        }

        private void CarregarUsuariosOnline()
        {
            try
            {
                var sessoes = _userSessionService.GetActiveSessions();
                UsuariosDataGrid.ItemsSource = sessoes;

                StatusTextBlock.Text = sessoes.Count > 0
                    ? $"{sessoes.Count} sessão(ões) ativa(s) no sistema."
                    : "Nenhuma sessão ativa no momento.";
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao carregar usuários online.", ex);
                StatusTextBlock.Text = $"Erro ao carregar usuários: {ex.Message}";
            }
        }

        private void AtualizarButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarUsuariosOnline();
        }

        private void LimparExpiradasButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _userSessionService.CleanExpiredSessions(60);
                CarregarUsuariosOnline();

                _logger.LogInfo("Sessões expiradas limpas pelo usuário.");
                MessageBox.Show(
                    "Sessões expiradas foram limpas com sucesso.",
                    "Limpeza concluída",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao limpar sessões expiradas.", ex);
                MessageBox.Show(
                    $"Erro ao limpar sessões expiradas:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
