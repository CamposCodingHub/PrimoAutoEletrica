using System;
using System.Windows;
using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.ExternalAi;

namespace PrimoAutoEletrica.Views
{
    public partial class ExternalAiSettingsWindow : Window
    {
        private readonly ExternalAssistantSettingsStore _store;
        private readonly PermissionService _permissions;

        public ExternalAiSettingsWindow()
        {
            InitializeComponent();
            WindowOwnerHelper.ConfigureOwner(this);
            _store = new ExternalAssistantSettingsStore(App.RuntimeAppDataPath);
            _permissions = PermissionService.CriarParaSessaoAtual(App.Logger);
            Loaded += (_, _) => LoadUi();
        }

        private void LoadUi()
        {
            if (!_permissions.TemPermissaoCodigo("ASSIST_CONFIGURAR") &&
                !_permissions.TemPermissaoCodigo("ASSIST_UTILIZAR") &&
                !string.Equals(App.Session?.CurrentUser?.PerfilAcesso, "Administrador", StringComparison.OrdinalIgnoreCase))
            {
                WindowInteractionHelper.ShowMessage(
                    "Permissão insuficiente para configurar IA externa (ASSIST_CONFIGURAR).",
                    "Acesso negado",
                    MessageBoxImage.Warning,
                    "ExternalAi");
                WindowInteractionHelper.CloseWithDialogResult(this, false, "ExternalAi");
                return;
            }

            var snap = _store.Load();
            EnabledCheckBox.IsChecked = snap.Enabled;
            KillSwitchCheckBox.IsChecked = snap.KillSwitch;
            ProviderNameTextBox.Text = snap.ProviderName;
            ModelIdTextBox.Text = snap.ModelId ?? string.Empty;
            ApiKeyEnvVarTextBox.Text = snap.ApiKeyEnvironmentVariable;

            var selector = new ExternalAssistantProviderSelector(_store.ToOptions(snap));
            StatusTextBlock.Text = selector.GetStatusLabel();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_permissions.TemPermissaoCodigo("ASSIST_CONFIGURAR") &&
                    !string.Equals(App.Session?.CurrentUser?.PerfilAcesso, "Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    WindowInteractionHelper.ShowMessage(
                        "ASSIST_CONFIGURAR necessária para salvar.",
                        "Acesso negado",
                        MessageBoxImage.Warning,
                        "ExternalAi");
                    return;
                }

                var snap = new ExternalAssistantSettingsSnapshot
                {
                    Enabled = EnabledCheckBox.IsChecked == true,
                    KillSwitch = KillSwitchCheckBox.IsChecked == true,
                    ProviderName = string.IsNullOrWhiteSpace(ProviderNameTextBox.Text)
                        ? "PRIMOX External AI"
                        : ProviderNameTextBox.Text.Trim(),
                    ModelId = string.IsNullOrWhiteSpace(ModelIdTextBox.Text) ? "gpt-4o-mini" : ModelIdTextBox.Text.Trim(),
                    ApiKeyEnvironmentVariable = ExternalAssistantOptions.DefaultApiKeyEnvironmentVariable,
                    BaseUrl = ExternalAssistantOptions.DefaultBaseUrl
                };

                var selector = new ExternalAssistantProviderSelector(_store.ToOptions(snap));
                snap.StatusLabel = selector.GetStatusLabel();
                _store.Save(snap);
                StatusTextBlock.Text = snap.StatusLabel;
                WindowInteractionHelper.CloseWithDialogResult(this, true, "ExternalAi");
            }
            catch (Exception ex)
            {
                WindowInteractionHelper.ShowMessage(ex.Message, "Falha ao salvar", MessageBoxImage.Error, "ExternalAi", ex);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            WindowInteractionHelper.CloseWithDialogResult(this, false, "ExternalAi");
        }
    }
}
