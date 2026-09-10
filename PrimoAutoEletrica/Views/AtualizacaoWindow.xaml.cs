using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

using PrimoAutoEletrica.Helpers;
namespace PrimoAutoEletrica.Views
{
    public partial class AtualizacaoWindow : Window
    {
        private readonly UpdateService _updateService;
        private readonly string _manifestPath;
        private UpdateManifest? _currentManifest;
        private readonly LoggerService? _logger;

        public AtualizacaoWindow(string appDataPath, string currentVersion, LoggerService? logger = null)
        {
            InitializeComponent();
            _logger = logger;
            _manifestPath = Path.Combine(appDataPath, "manifest.json");
            _updateService = new UpdateService(appDataPath, currentVersion, logger);
            
            CurrentVersionText.Text = $"Versão atual: {currentVersion}";
            
            _updateService.StatusChanged += OnStatusChanged;
            _updateService.ProgressChanged += OnProgressChanged;
        }

        private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckUpdateButton.IsEnabled = false;
                ProgressText.Text = "Verificando atualizações...";
                ProgressBar.Value = 0;

                _currentManifest = await _updateService.CheckForUpdatesAsync(_manifestPath);

                if (_currentManifest == null)
                {
                    MessageBox.Show("Nenhuma atualização disponível ou erro ao verificar.", 
                                  "Verificação de Atualização", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    ProgressText.Text = "Nenhuma atualização disponível";
                    return;
                }

                if (_currentManifest.UpdateAvailable)
                {
                    UpdateAvailableText.Text = $"Atualização disponível: {_currentManifest.LatestVersion}";
                    ChangesList.ItemsSource = _currentManifest.Changes;
                    InstallUpdateButton.IsEnabled = true;
                    ProgressText.Text = "Atualização disponível";
                    ProgressBar.Value = 100;
                }
                else
                {
                    MessageBox.Show("Você já está usando a versão mais recente.", 
                                  "Verificação de Atualização", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Information);
                    ProgressText.Text = "Sistema atualizado";
                    ProgressBar.Value = 100;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao verificar atualização: {ex.Message}", ex);
                MessageBox.Show($"Erro ao verificar atualização: {ex.Message}", 
                              UiText.T("Error"), 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                ProgressText.Text = "Erro na verificação";
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
        }

        private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentManifest == null)
            {
                MessageBox.Show("Selecione uma atualização primeiro.", 
                              "Atualização", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Deseja instalar a versão {_currentManifest.LatestVersion}?\n\n" +
                "O sistema será fechado durante a atualização.\n" +
                "Um backup automático será criado antes da atualização.",
                "Confirmar Atualização",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                InstallUpdateButton.IsEnabled = false;
                CheckUpdateButton.IsEnabled = false;
                CancelButton.IsEnabled = false;

                var updateResult = await _updateService.ApplyUpdateAsync(_currentManifest);

                if (updateResult.Success)
                {
                    MessageBox.Show(
                        $"Atualização concluída com sucesso!\n\n" +
                        $"Versão: {_currentManifest.LatestVersion}\n" +
                        $"Backup: {updateResult.BackupPath}",
                        "Atualização Concluída",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show(
                        $"Falha na atualização:\n{updateResult.Message}",
                        "Erro na Atualização",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    if (!string.IsNullOrEmpty(updateResult.BackupPath))
                    {
                        var rollbackResult = MessageBox.Show(
                            "Deseja fazer rollback para a versão anterior?",
                            "Rollback",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (rollbackResult == MessageBoxResult.Yes)
                        {
                            await _updateService.RollbackAsync(updateResult.BackupPath);
                            MessageBox.Show("Rollback concluído.", "Rollback", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao instalar atualização: {ex.Message}", ex);
                MessageBox.Show($"Erro ao instalar atualização: {ex.Message}", 
                              UiText.T("Error"), 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
            finally
            {
                InstallUpdateButton.IsEnabled = true;
                CheckUpdateButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnStatusChanged(object? sender, UpdateStatus status)
        {
            Dispatcher.Invoke(() =>
            {
                switch (status)
                {
                    case UpdateStatus.Checking:
                        ProgressText.Text = "Verificando atualizações...";
                        ProgressBar.Value = 10;
                        break;
                    case UpdateStatus.Validating:
                        ProgressText.Text = "Validando pacote...";
                        ProgressBar.Value = 30;
                        break;
                    case UpdateStatus.CreatingBackup:
                        ProgressText.Text = "Criando backup...";
                        ProgressBar.Value = 50;
                        break;
                    case UpdateStatus.ApplyingUpdate:
                        ProgressText.Text = "Aplicando atualização...";
                        ProgressBar.Value = 70;
                        break;
                    case UpdateStatus.ValidatingUpdate:
                        ProgressText.Text = "Validando atualização...";
                        ProgressBar.Value = 90;
                        break;
                    case UpdateStatus.Completed:
                        ProgressText.Text = "Atualização concluída!";
                        ProgressBar.Value = 100;
                        break;
                    case UpdateStatus.Failed:
                        ProgressText.Text = "Falha na atualização";
                        ProgressBar.Value = 0;
                        break;
                    case UpdateStatus.RollingBack:
                        ProgressText.Text = "Executando rollback...";
                        ProgressBar.Value = 50;
                        break;
                }
            });
        }

        private void OnProgressChanged(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressText.Text = message;
            });
        }
    }
}
