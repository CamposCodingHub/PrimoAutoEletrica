using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;

using PrimoAutoEletrica.Helpers;
namespace PrimoAutoEletrica.Views
{
    public partial class BackupSettingsWindow : Window
    {
        private readonly string _appDataPath;
        private readonly BackupSettings _settings;
        private readonly ExternalBackupService _backupService;
        private readonly LoggerService? _logger;

        public BackupSettingsWindow(string appDataPath, BackupSettings settings, LoggerService? logger = null)
        {
            InitializeComponent();
            _appDataPath = appDataPath;
            _settings = settings;
            _logger = logger;
            _backupService = new ExternalBackupService(appDataPath, settings, logger);

            LoadSettings();
        }

        private void LoadSettings()
        {
            // Auto Backup
            AutoBackupEnabledCheckBox.IsChecked = _settings.AutoBackup.Enabled;
            FrequencyComboBox.SelectedValue = _settings.AutoBackup.Frequency;
            BackupTimeTextBox.Text = _settings.AutoBackup.Time;
            BeforeUpdateCheckBox.IsChecked = _settings.AutoBackup.BeforeUpdate;
            BeforeMigrationCheckBox.IsChecked = _settings.AutoBackup.BeforeMigration;
            IncludeMediaCheckBox.IsChecked = _settings.AutoBackup.IncludeMedia;

            // External Backup
            ExternalBackupEnabledCheckBox.IsChecked = _settings.ExternalBackup.Enabled;
            ExternalPathTextBox.Text = _settings.ExternalBackup.Destination;
            SyncCloudCheckBox.IsChecked = _settings.ExternalBackup.SyncWithCloud;
            CloudProviderComboBox.SelectedValue = _settings.ExternalBackup.CloudProvider;
            EncryptBackupCheckBox.IsChecked = _settings.ExternalBackup.Encrypt;

            // Compression
            CompressionEnabledCheckBox.IsChecked = _settings.Compression.Enabled;
            CompressionLevelComboBox.SelectedValue = _settings.Compression.Level;

            // Retention
            DailyRetentionTextBox.Text = _settings.Retention.DailyBackups.ToString();
            WeeklyRetentionTextBox.Text = _settings.Retention.WeeklyBackups.ToString();
            MonthlyRetentionTextBox.Text = _settings.Retention.MonthlyBackups.ToString();
            AutoCleanCheckBox.IsChecked = _settings.Retention.AutoClean;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Auto Backup
                _settings.AutoBackup.Enabled = AutoBackupEnabledCheckBox.IsChecked ?? false;
                _settings.AutoBackup.Frequency = FrequencyComboBox.SelectedValue?.ToString() ?? "daily";
                _settings.AutoBackup.Time = BackupTimeTextBox.Text;
                _settings.AutoBackup.BeforeUpdate = BeforeUpdateCheckBox.IsChecked ?? false;
                _settings.AutoBackup.BeforeMigration = BeforeMigrationCheckBox.IsChecked ?? false;
                _settings.AutoBackup.IncludeMedia = IncludeMediaCheckBox.IsChecked ?? false;

                // External Backup
                _settings.ExternalBackup.Enabled = ExternalBackupEnabledCheckBox.IsChecked ?? false;
                _settings.ExternalBackup.Destination = ExternalPathTextBox.Text;
                _settings.ExternalBackup.SyncWithCloud = SyncCloudCheckBox.IsChecked ?? false;
                _settings.ExternalBackup.CloudProvider = CloudProviderComboBox.SelectedValue?.ToString() ?? "none";
                _settings.ExternalBackup.Encrypt = EncryptBackupCheckBox.IsChecked ?? false;

                // Compression
                _settings.Compression.Enabled = CompressionEnabledCheckBox.IsChecked ?? false;
                _settings.Compression.Level = CompressionLevelComboBox.SelectedValue?.ToString() ?? "normal";

                // Retention
                if (int.TryParse(DailyRetentionTextBox.Text, out int daily))
                    _settings.Retention.DailyBackups = daily;
                if (int.TryParse(WeeklyRetentionTextBox.Text, out int weekly))
                    _settings.Retention.WeeklyBackups = weekly;
                if (int.TryParse(MonthlyRetentionTextBox.Text, out int monthly))
                    _settings.Retention.MonthlyBackups = monthly;
                _settings.Retention.AutoClean = AutoCleanCheckBox.IsChecked ?? false;

                // Salvar configurações
                var configPath = Path.Combine(_appDataPath, "Config", "backup-settings.json");
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                var json = System.Text.Json.JsonSerializer.Serialize(_settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);

                _logger?.LogInfo("Configurações de backup salvas");

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao salvar configurações: {ex.Message}", ex);
                MessageBox.Show($"Erro ao salvar configurações: {ex.Message}", UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Simples input dialog para selecionar diretório
            var inputDialog = new Window
            {
                Title = "Selecionar diretório de backup externo",
                Width = 500,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel();
            stackPanel.Margin = new Thickness(10);

            var label = new Label
            {
                Content = "Digite o caminho do diretório:"
            };
            stackPanel.Children.Add(label);

            var textBox = new TextBox
            {
                Text = ExternalPathTextBox.Text,
                Margin = new Thickness(0, 5, 0, 10)
            };
            stackPanel.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 10, 0)
            };
            okButton.Click += (s, e) => 
            {
                inputDialog.DialogResult = true;
                inputDialog.Close();
            };
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "Cancelar",
                Width = 80
            };
            cancelButton.Click += (s, e) => 
            {
                inputDialog.DialogResult = false;
                inputDialog.Close();
            };
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(buttonPanel);
            inputDialog.Content = stackPanel;

            if (inputDialog.ShowDialog() == true)
            {
                var path = textBox.Text?.Trim();
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    ExternalPathTextBox.Text = path;
                }
                else if (!string.IsNullOrEmpty(path))
                {
                    MessageBox.Show("O diretório não existe. Por favor, verifique o caminho.", "Diretório inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void TestBackupButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TestBackupButton.IsEnabled = false;
                TestBackupButton.Content = "Testando...";

                var result = await _backupService.CreateBackupAsync("Teste de configurações", true);

                if (result.Success)
                {
                    MessageBox.Show(
                        $"Backup de teste criado com sucesso!\n\n" +
                        $"Caminho: {result.BackupPath}\n" +
                        $"Tamanho: {FormatSize(result.CompressedSize)}\n" +
                        $"Razão de compressão: {result.CompressionRatio:P2}",
                        "Teste de Backup",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"Falha no backup de teste:\n{result.Message}",
                        UiText.T("Error"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro no teste de backup: {ex.Message}", ex);
                MessageBox.Show($"Erro no teste de backup: {ex.Message}", UiText.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestBackupButton.IsEnabled = true;
                TestBackupButton.Content = "Testar Backup";
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
