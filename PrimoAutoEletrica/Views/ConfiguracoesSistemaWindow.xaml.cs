using PrimoAutoEletrica.Helpers;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PrimoAutoEletrica.Views
{
    public partial class ConfiguracoesSistemaWindow : Window
    {
        private readonly Funcionario _funcionarioLogado;
        private readonly LoggerService _logger;
        private DatabaseConnectionSettings _settings;
        private StationConfiguration _stationConfiguration;
        private BusinessConfiguration _businessConfiguration;
        private SystemConfiguration _systemConfiguration;
        private readonly PermissionService _permissionService;
        private readonly SystemConfigurationService _systemConfigurationService;
        private readonly ThemeService _themeService = new();
        private readonly PrinterDiagnosticsService _printerDiagnosticsService = new();

        public ConfiguracoesSistemaWindow(Funcionario funcionarioLogado)
        {
            InitializeComponent();

            _funcionarioLogado = funcionarioLogado ?? throw new ArgumentNullException(nameof(funcionarioLogado));
            _logger = App.Logger;
            _permissionService = new PermissionService(_funcionarioLogado, _logger, App.Database);
            _systemConfigurationService = new SystemConfigurationService(App.Database, _logger);
            _settings = DatabaseConnectionSettingsService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
            _stationConfiguration = StationService.GetConfiguration(App.RuntimeAppDataPath);
            _systemConfiguration = _systemConfigurationService.LoadOrCreate(App.RuntimeAppDataPath);
            var legacyBusinessConfiguration = BusinessConfigurationService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);
            if (legacyBusinessConfiguration.UpdatedAt > _systemConfiguration.UpdatedAt)
            {
                _businessConfiguration = legacyBusinessConfiguration;
                AtualizarConfiguracaoSistemaComComercial(_businessConfiguration);
                _systemConfigurationService.SaveTrusted(_systemConfiguration, _funcionarioLogado.Nome);
            }
            else
            {
                _businessConfiguration = _systemConfiguration.ToBusinessConfiguration();
            }

            Loaded += ConfiguracoesSistemaWindow_Loaded;

            CarregarConfiguracoes();
            CarregarResumoAmbiente();
            CarregarConfiguracoesComerciais();
            CarregarInformacoesBackup();
            CarregarConfiguracoesOperacionais();
            CarregarConfiguracoesMultiusuario();
            CarregarConfiguracoesImpressaoPdv();
        }

        private void ConfiguracoesSistemaWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarConfiguracoesMultiusuario();
            CarregarConfiguracoesImpressaoPdv();
        }

        public DatabaseConnectionSettings? ConfiguracoesSalvas { get; private set; }

        private void SalvarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGarantirPermissaoConfiguracao("salvar configuracoes gerais"))
            {
                return;
            }

            if (!TryValidarCampos(out var timeoutInatividade, out var timeoutBanco))
            {
                return;
            }

            if (!TryBuildBusinessConfigurationFromUi(out var businessConfiguration))
            {
                return;
            }

            if (!TryBuildSystemConfigurationFromUi(businessConfiguration, out var systemConfiguration))
            {
                return;
            }

            var networkBackupDir = string.IsNullOrWhiteSpace(NetworkBackupFolderTextBox.Text.Trim())
                ? null
                : NetworkBackupFolderTextBox.Text.Trim();

            var useSQLite = SQLiteRadioButton.IsChecked == true;
            var sqlServerHost = SqlServerTextBox.Text.Trim();
            var sqlServerInstance = SqlServerInstanceTextBox.Text.Trim();
            var sqlServerDatabase = SqlServerDatabaseTextBox.Text.Trim();
            var useWindowsAuth = WindowsAuthRadioButton.IsChecked == true;
            var sqlServerUser = SqlServerUserTextBox.Text.Trim();
            var sqlServerPassword = SqlServerPasswordBox.Password;

            if (!useSQLite && AllowFallbackSQLiteCheckBox.IsChecked != true)
            {
                MessageBox.Show(
                    "SQL Server ainda nao esta habilitado como runtime operacional completo.\n\n" +
                    "Voce pode testar a conexao SQL Server nesta tela, mas para salvar SQL Server como provider ativo durante a migracao e necessario marcar explicitamente o fallback para SQLite.\n\n" +
                    "Sem o fallback explicito, o sistema bloqueia a configuracao para evitar a falsa impressao de que esta operando em SQL Server.",
                    "SQL Server em migracao",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var provider = useSQLite ? "SQLite" : "SqlServer";

            var configuracoesAtualizadas = new DatabaseConnectionSettings
            {
                Provider = provider,
                SQLitePath = _settings.SQLitePath,
                SqlServerHost = useSQLite ? string.Empty : (sqlServerHost ?? string.Empty),
                SqlServerInstance = useSQLite ? string.Empty : (sqlServerInstance ?? string.Empty),
                SqlServerDatabase = useSQLite ? string.Empty : (sqlServerDatabase ?? string.Empty),
                UseWindowsAuthentication = useWindowsAuth,
                SqlServerUsername = useWindowsAuth ? string.Empty : (sqlServerUser ?? string.Empty),
                SqlServerPassword = useWindowsAuth ? string.Empty : (sqlServerPassword ?? string.Empty),
                AllowUnsupportedSqlServerRuntimeFallback = !useSQLite && AllowFallbackSQLiteCheckBox.IsChecked == true,
                CommandTimeoutSeconds = timeoutBanco,
                SessionInactivityTimeoutMinutes = timeoutInatividade,
                NetworkBackupDirectory = networkBackupDir
            };

            // Salvar configurações de multiusuário
            try
            {
                DatabaseConnectionSettingsService.Save(App.RuntimeAppDataPath, configuracoesAtualizadas);
                BusinessConfigurationService.Save(App.RuntimeAppDataPath, businessConfiguration);
                _systemConfigurationService.SaveAuthorized(systemConfiguration, _funcionarioLogado, _permissionService);
                AplicarTemaPreferido(systemConfiguration.PreferredTheme);
                SalvarConfiguracoesMultiusuario();
                _businessConfiguration = businessConfiguration;
                _systemConfiguration = systemConfiguration;
                ConfiguracoesSalvas = DatabaseConnectionSettingsService.LoadOrCreateDefault(App.RuntimeAppDataPath, _logger);

                App.Audit.Registrar(
                    categoria: "Sistema",
                    acao: "AtualizarConfiguracoes",
                    entidade: "Configuracoes",
                    entidadeId: "database-settings.json",
                    detalhes: $"Provider={provider}; TimeoutInatividade={timeoutInatividade}min; TimeoutBanco={timeoutBanco}s; NetworkBackup={networkBackupDir ?? "Nao configurado"}",
                    valorAnterior: $"Provider={_settings.Provider}; TimeoutInatividade={_settings.SessionInactivityTimeoutMinutes}min; TimeoutBanco={_settings.CommandTimeoutSeconds}s; NetworkBackup={_settings.NetworkBackupDirectory ?? "Nao configurado"}",
                    valorNovo: $"Provider={ConfiguracoesSalvas.Provider}; TimeoutInatividade={ConfiguracoesSalvas.SessionInactivityTimeoutMinutes}min; TimeoutBanco={ConfiguracoesSalvas.CommandTimeoutSeconds}s; NetworkBackup={ConfiguracoesSalvas.NetworkBackupDirectory ?? "Nao configurado"}");

                App.Audit.Registrar(
                    categoria: "Sistema",
                    acao: "AtualizarConfiguracaoComercial",
                    entidade: "Configuracoes",
                    entidadeId: BusinessConfigurationService.GetConfigurationFilePath(App.RuntimeAppDataPath),
                    detalhes: $"Empresa={businessConfiguration.CompanyDisplayName}; LogoConfigurado={!string.IsNullOrWhiteSpace(businessConfiguration.LogoPath)}; Rodape={businessConfiguration.ReceiptFooter.Length} chars");

                App.Audit.Registrar(
                    categoria: "Sistema",
                    acao: "AtualizarConfiguracaoOperacional",
                    entidade: "ConfiguracoesSistema",
                    entidadeId: "ConfiguracoesSistema",
                    detalhes: $"Tema={systemConfiguration.PreferredTheme}; AutoBackup={systemConfiguration.AutoBackupEnabled}; OS={systemConfiguration.OsNumberPrefix}/{systemConfiguration.OsNextNumber}; Orcamento={systemConfiguration.OrcamentoNumberPrefix}/{systemConfiguration.OrcamentoNextNumber}; Garantia={systemConfiguration.DefaultWarrantyDays}; Margem={systemConfiguration.DefaultProductMarginPercent:N2}");

                _logger.LogInfo(
                    $"Configuracoes do sistema atualizadas por '{_funcionarioLogado.Email}'. Provider={provider}; TimeoutInatividade={ConfiguracoesSalvas.SessionInactivityTimeoutMinutes}min; TimeoutBanco={ConfiguracoesSalvas.CommandTimeoutSeconds}s; NetworkBackup={ConfiguracoesSalvas.NetworkBackupDirectory ?? "Nao configurado"}.");

                var providerMessage = ConfiguracoesSalvas.IsSqlServer && ConfiguracoesSalvas.AllowUnsupportedSqlServerRuntimeFallback
                    ? "\n\nAtencao: SQL Server foi salvo apenas como provider preparado, com fallback explicito. O runtime operacional continua SQLite ate a migracao completa da camada de persistencia."
                    : "\n\nA mudanca de provider de banco requer reinicializacao do sistema.";

                MessageBox.Show(
                    "Configuracoes salvas com sucesso.\n\nO timeout de inatividade foi preparado para ser reaplicado na sessao atual.\nO timeout do banco sera usado em novas conexoes apos reabrir o sistema.\nA pasta de rede de backup sera usada nos proximos backups." + providerMessage,
                    "Configuracoes salvas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao salvar configuracoes do sistema.", ex);
                MessageBox.Show(
                    $"Erro ao salvar configuracoes:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void FecharButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GerenciarPerfisButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGarantirPermissaoConfiguracao("gerenciar perfis"))
            {
                return;
            }

            var window = new GerenciarPerfisWindow(_funcionarioLogado)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void ConfigurarPermissoesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGarantirPermissaoConfiguracao("configurar permissoes"))
            {
                return;
            }

            var window = new ConfigurarPermissoesWindow(_funcionarioLogado)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void SalvarConfiguracoesComerciaisButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGarantirPermissaoConfiguracao("salvar dados comerciais"))
            {
                return;
            }

            if (!TryBuildBusinessConfigurationFromUi(out var configuration))
            {
                return;
            }

            try
            {
                AtualizarConfiguracaoSistemaComComercial(configuration);
                _systemConfigurationService.SaveAuthorized(_systemConfiguration, _funcionarioLogado, _permissionService);
                BusinessConfigurationService.Save(App.RuntimeAppDataPath, configuration);
                _businessConfiguration = configuration;
                AtualizarStatusLogo(configuration.LogoPath);
                ReceiptPreviewTextBlock.Text = $"Marca/comprovante salvos em {DateTime.Now:dd/MM/yyyy HH:mm}. {BuildReceiptPreview(configuration)}";

                App.Audit.Registrar(
                    categoria: "Sistema",
                    acao: "AtualizarConfiguracaoComercial",
                    entidade: "Configuracoes",
                    entidadeId: BusinessConfigurationService.GetConfigurationFilePath(App.RuntimeAppDataPath),
                    detalhes: $"Empresa={configuration.CompanyDisplayName}; LogoConfigurado={!string.IsNullOrWhiteSpace(configuration.LogoPath)}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao salvar configuracao comercial.", ex);
                ReceiptPreviewTextBlock.Text = $"Erro ao salvar marca/comprovante: {ex.Message}";
            }
        }

        private void ValidarLogoButton_Click(object sender, RoutedEventArgs e)
        {
            AtualizarStatusLogo(CompanyLogoPathTextBox.Text.Trim());
        }

        private void GerarPreviaComprovanteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryBuildBusinessConfigurationFromUi(out var configuration))
            {
                return;
            }

            ReceiptPreviewTextBlock.Text = BuildReceiptPreview(configuration);
        }

        private void CriarBackupManualButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var caminhoBackup = App.Backups.CriarBackupManual();
                var verificado = App.Backups.VerificarBackup(caminhoBackup);
                BackupStatusTextBlock.Text = verificado
                    ? $"Backup manual criado e verificado com sucesso em {DateTime.Now:dd/MM/yyyy HH:mm}."
                    : $"Backup manual criado em {DateTime.Now:dd/MM/yyyy HH:mm}, mas a verificacao retornou inconsistencias.";

                MessageBox.Show(
                    verificado
                        ? $"Backup criado e verificado com sucesso:\n{caminhoBackup}"
                        : $"Backup criado em:\n{caminhoBackup}\n\nA verificacao automatica encontrou inconsistencias e registrou o evento em auditoria.",
                    verificado ? "Backup concluido" : "Backup com alerta",
                    MessageBoxButton.OK,
                    verificado ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao criar backup manual a partir da tela de configuracoes.", ex);
                MessageBox.Show(
                    $"Erro ao criar backup manual:\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ValidarBackupRestauracaoButton_Click(object sender, RoutedEventArgs e)
        {
            TryValidarBackupParaRestauracao(out _);
        }

        private void RestaurarBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidarBackupParaRestauracao(out var caminhoBackup))
            {
                return;
            }

            var restauracaoAutomatizadaSegura = App.IsSmokeTestMode && App.IsIsolatedAutomatedAppData;
            var confirmacao = restauracaoAutomatizadaSegura
                ? MessageBoxResult.Yes
                : MessageBox.Show(
                    "A restauracao substituira o banco atual pelo backup informado.\n\nO sistema criara um backup de seguranca antes da troca, mas todos os outros usuarios devem estar fora do sistema.\n\nDeseja continuar?",
                    "Confirmar restauracao de backup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

            if (confirmacao != MessageBoxResult.Yes)
            {
                RestoreBackupStatusTextBlock.Text = "Restauracao cancelada pelo usuario apos validacao do backup.";
                return;
            }

            try
            {
                var backupSeguranca = App.Backups.RestaurarBackup(caminhoBackup);
                RestoreBackupStatusTextBlock.Text = string.IsNullOrWhiteSpace(backupSeguranca)
                    ? $"Backup restaurado com sucesso em {DateTime.Now:dd/MM/yyyy HH:mm}. Reinicie o sistema antes de continuar operando."
                    : $"Backup restaurado com sucesso em {DateTime.Now:dd/MM/yyyy HH:mm}. Backup de seguranca criado em: {backupSeguranca}. Reinicie o sistema antes de continuar operando.";
                RestoreBackupStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;

                WindowInteractionHelper.ShowMessage(
                    "Backup restaurado com sucesso.\n\nReinicie o sistema antes de continuar operando.",
                    "Restauracao concluida",
                    MessageBoxImage.Information,
                    "Configuracoes");
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao restaurar backup pela tela de configuracoes.", ex);
                RestoreBackupStatusTextBlock.Text = $"Falha ao restaurar backup: {ex.Message}";
                RestoreBackupStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void AbrirPastaButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string caminho } || string.IsNullOrWhiteSpace(caminho))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(caminho);
                Process.Start(new ProcessStartInfo
                {
                    FileName = caminho,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao abrir pasta '{caminho}'.", ex);
                MessageBox.Show(
                    $"Nao foi possivel abrir a pasta:\n{caminho}\n\n{ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CarregarConfiguracoes()
        {
            SessionTimeoutTextBox.Text = _settings.SessionInactivityTimeoutMinutes.ToString();
            CommandTimeoutTextBox.Text = _settings.CommandTimeoutSeconds.ToString();
            DataFolderTextBox.Text = App.RuntimeAppDataPath;
            LogFolderTextBox.Text = App.RuntimeLogDirectory;
            BackupFolderTextBox.Text = App.RuntimeBackupDirectory;
            DatabaseProviderTextBox.Text = _settings.Provider;
            DatabaseTargetTextBox.Text = _settings.IsSQLite
                ? _settings.ResolveSqlitePath(App.RuntimeAppDataPath)
                : $"{_settings.SqlServerHost} / {_settings.SqlServerDatabase}";
            SettingsFileTextBox.Text = DatabaseConnectionSettingsService.GetSettingsFilePath(App.RuntimeAppDataPath);
            NetworkBackupFolderTextBox.Text = _settings.NetworkBackupDirectory ?? string.Empty;

            SQLiteRadioButton.IsChecked = _settings.IsSQLite;
            SQLServerRadioButton.IsChecked = !_settings.IsSQLite;
            SqlServerTextBox.Text = _settings.SqlServerHost ?? "localhost";
            SqlServerInstanceTextBox.Text = _settings.SqlServerInstance ?? string.Empty;
            SqlServerDatabaseTextBox.Text = _settings.SqlServerDatabase ?? "PrimoAutoEletrica";
            SqlServerTimeoutTextBox.Text = _settings.CommandTimeoutSeconds.ToString();
            WindowsAuthRadioButton.IsChecked = _settings.UseWindowsAuthentication;
            SqlAuthRadioButton.IsChecked = !_settings.UseWindowsAuthentication;
            SqlServerUserTextBox.Text = _settings.SqlServerUsername ?? string.Empty;
            AllowFallbackSQLiteCheckBox.IsChecked = _settings.AllowUnsupportedSqlServerRuntimeFallback;
        }

        private void CarregarConfiguracoesComerciais()
        {
            CompanyDisplayNameTextBox.Text = _businessConfiguration.CompanyDisplayName;
            CompanyLegalNameTextBox.Text = _businessConfiguration.CompanyLegalName;
            CompanyDocumentTextBox.Text = _businessConfiguration.CompanyDocument;
            CompanyPhoneTextBox.Text = _businessConfiguration.CompanyPhone;
            CompanyWhatsAppTextBox.Text = _businessConfiguration.CompanyWhatsApp;
            CompanyAddressTextBox.Text = _businessConfiguration.CompanyAddress;
            CompanyLogoPathTextBox.Text = _businessConfiguration.LogoPath;
            ReceiptHeaderTextBox.Text = _businessConfiguration.ReceiptHeader;
            ReceiptFooterTextBox.Text = _businessConfiguration.ReceiptFooter;
            AtualizarStatusLogo(_businessConfiguration.LogoPath);
            ReceiptPreviewTextBlock.Text = BuildReceiptPreview(_businessConfiguration);
        }

        private void CarregarConfiguracoesOperacionais()
        {
            SelecionarComboBoxPorTag(ThemeModeComboBox, _systemConfiguration.PreferredTheme);
            AutoBackupEnabledCheckBox.IsChecked = _systemConfiguration.AutoBackupEnabled;
            AutoBackupRetentionTextBox.Text = _systemConfiguration.AutoBackupRetentionCopies.ToString(CultureInfo.InvariantCulture);
            OsNumberPrefixTextBox.Text = _systemConfiguration.OsNumberPrefix;
            OsNextNumberTextBox.Text = _systemConfiguration.OsNextNumber.ToString(CultureInfo.InvariantCulture);
            OrcamentoNumberPrefixTextBox.Text = _systemConfiguration.OrcamentoNumberPrefix;
            OrcamentoNextNumberTextBox.Text = _systemConfiguration.OrcamentoNextNumber.ToString(CultureInfo.InvariantCulture);
            DefaultWarrantyDaysTextBox.Text = _systemConfiguration.DefaultWarrantyDays.ToString(CultureInfo.InvariantCulture);
            DefaultProductMarginTextBox.Text = _systemConfiguration.DefaultProductMarginPercent.ToString("N2", CultureInfo.CurrentCulture);
            MessageTemplateOrcamentoTextBox.Text = _systemConfiguration.MessageTemplateOrcamento;
            MessageTemplateOrdemProntaTextBox.Text = _systemConfiguration.MessageTemplateOrdemPronta;
            MessageTemplateGarantiaTextBox.Text = _systemConfiguration.MessageTemplateGarantia;
            PermissionPolicyStatusTextBlock.Text = $"{_systemConfiguration.PermissionPolicySummary} Alteracoes criticas exigem SISTEMA_CONFIGURAR.";
            OperationalSettingsStatusTextBlock.Text = $"Configuracoes operacionais carregadas do banco em {DateTime.Now:dd/MM/yyyy HH:mm}.";
        }

        private void SalvarConfiguracoesOperacionaisButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGarantirPermissaoConfiguracao("salvar configuracoes operacionais"))
            {
                return;
            }

            if (!TryBuildBusinessConfigurationFromUi(out var businessConfiguration))
            {
                return;
            }

            if (!TryBuildSystemConfigurationFromUi(businessConfiguration, out var systemConfiguration))
            {
                return;
            }

            try
            {
                _systemConfigurationService.SaveAuthorized(systemConfiguration, _funcionarioLogado, _permissionService);
                BusinessConfigurationService.Save(App.RuntimeAppDataPath, businessConfiguration);
                AplicarTemaPreferido(systemConfiguration.PreferredTheme);
                _systemConfiguration = systemConfiguration;
                _businessConfiguration = businessConfiguration;
                OperationalSettingsStatusTextBlock.Text = $"Configuracoes operacionais salvas no banco em {DateTime.Now:dd/MM/yyyy HH:mm}.";

                App.Audit.Registrar(
                    categoria: "Sistema",
                    acao: "AtualizarConfiguracaoOperacional",
                    entidade: "ConfiguracoesSistema",
                    entidadeId: "ConfiguracoesSistema",
                    detalhes: $"Tema={systemConfiguration.PreferredTheme}; AutoBackup={systemConfiguration.AutoBackupEnabled}; Retencao={systemConfiguration.AutoBackupRetentionCopies}; Garantia={systemConfiguration.DefaultWarrantyDays}; Margem={systemConfiguration.DefaultProductMarginPercent:N2}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao salvar configuracoes operacionais.", ex);
                OperationalSettingsStatusTextBlock.Text = $"Erro ao salvar configuracoes operacionais: {ex.Message}";
            }
        }

        private void CarregarResumoAmbiente()
        {
            var versao = Assembly.GetExecutingAssembly().GetName().Version;
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var dataBuild = File.Exists(assemblyPath)
                ? File.GetLastWriteTime(assemblyPath)
                : DateTime.Now;

            UsuarioAtualTextBlock.Text = _funcionarioLogado.Nome;
            PerfilAtualTextBlock.Text = _funcionarioLogado.PerfilAcesso;
            VersaoAtualTextBlock.Text = versao == null
                ? "v0.0.0"
                : $"v{versao.Major}.{versao.Minor}.{Math.Max(versao.Build, 0)}";
            BuildAtualTextBlock.Text = dataBuild.ToString("dd/MM/yyyy HH:mm");
            AmbienteAtualTextBlock.Text = App.RuntimeModeName;
            BancoAtualTextBlock.Text = _settings.IsSQLite
                ? $"SQLite ({Path.GetFileName(App.Database.DatabasePath)})"
                : $"{_settings.Provider} ({_settings.SqlServerHost}/{_settings.SqlServerDatabase})";
        }

        private bool TryValidarCampos(out int timeoutInatividade, out int timeoutBanco)
        {
            timeoutInatividade = 0;
            timeoutBanco = 0;

            if (!int.TryParse(SessionTimeoutTextBox.Text.Trim(), out timeoutInatividade))
            {
                return ExibirErroValidacao("Informe um timeout de inatividade valido em minutos.", SessionTimeoutTextBox);
            }

            if (!int.TryParse(CommandTimeoutTextBox.Text.Trim(), out timeoutBanco))
            {
                return ExibirErroValidacao("Informe um timeout de banco valido em segundos.", CommandTimeoutTextBox);
            }

            var erroTimeoutInatividade = CadastroValidationHelper.ValidarInteiro(timeoutInatividade, "um timeout de inatividade", permitirZero: false);
            if (!string.IsNullOrWhiteSpace(erroTimeoutInatividade))
            {
                return ExibirErroValidacao(erroTimeoutInatividade, SessionTimeoutTextBox);
            }

            if (timeoutInatividade < DatabaseConnectionSettings.MinimumSessionInactivityTimeoutMinutes ||
                timeoutInatividade > DatabaseConnectionSettings.MaximumSessionInactivityTimeoutMinutes)
            {
                return ExibirErroValidacao(
                    $"Informe um timeout de inatividade entre {DatabaseConnectionSettings.MinimumSessionInactivityTimeoutMinutes} e {DatabaseConnectionSettings.MaximumSessionInactivityTimeoutMinutes} minutos.",
                    SessionTimeoutTextBox);
            }

            var erroTimeoutBanco = CadastroValidationHelper.ValidarInteiro(timeoutBanco, "um timeout de banco", permitirZero: false);
            if (!string.IsNullOrWhiteSpace(erroTimeoutBanco))
            {
                return ExibirErroValidacao(erroTimeoutBanco, CommandTimeoutTextBox);
            }

            if (timeoutBanco < DatabaseConnectionSettings.MinimumCommandTimeoutSeconds ||
                timeoutBanco > DatabaseConnectionSettings.MaximumCommandTimeoutSeconds)
            {
                return ExibirErroValidacao(
                    $"Informe um timeout de banco entre {DatabaseConnectionSettings.MinimumCommandTimeoutSeconds} e {DatabaseConnectionSettings.MaximumCommandTimeoutSeconds} segundos.",
                    CommandTimeoutTextBox);
            }

            return true;
        }

        private bool TryBuildBusinessConfigurationFromUi(out BusinessConfiguration configuration)
        {
            configuration = new BusinessConfiguration
            {
                CompanyDisplayName = CompanyDisplayNameTextBox.Text.Trim(),
                CompanyLegalName = CompanyLegalNameTextBox.Text.Trim(),
                CompanyDocument = CompanyDocumentTextBox.Text.Trim(),
                CompanyPhone = CompanyPhoneTextBox.Text.Trim(),
                CompanyWhatsApp = CompanyWhatsAppTextBox.Text.Trim(),
                CompanyAddress = CompanyAddressTextBox.Text.Trim(),
                LogoPath = CompanyLogoPathTextBox.Text.Trim(),
                ReceiptHeader = ReceiptHeaderTextBox.Text.Trim(),
                ReceiptFooter = ReceiptFooterTextBox.Text.Trim()
            };

            if (string.IsNullOrWhiteSpace(configuration.CompanyDisplayName))
            {
                return ExibirErroValidacao("Informe o nome da empresa que aparecera no comprovante.", CompanyDisplayNameTextBox);
            }

            AtualizarStatusLogo(configuration.LogoPath);
            return true;
        }

        private bool TryBuildSystemConfigurationFromUi(BusinessConfiguration businessConfiguration, out SystemConfiguration configuration)
        {
            configuration = new SystemConfiguration();

            if (!TryParsePositiveInt(AutoBackupRetentionTextBox.Text, "uma retencao de backup automatico", AutoBackupRetentionTextBox, out var autoBackupRetention) ||
                !TryParsePositiveInt(OsNextNumberTextBox.Text, "o proximo numero de OS", OsNextNumberTextBox, out var osNextNumber) ||
                !TryParsePositiveInt(OrcamentoNextNumberTextBox.Text, "o proximo numero de orcamento", OrcamentoNextNumberTextBox, out var orcamentoNextNumber) ||
                !TryParseNonNegativeInt(DefaultWarrantyDaysTextBox.Text, "a garantia padrao", DefaultWarrantyDaysTextBox, out var warrantyDays) ||
                !TryParseNonNegativeDecimal(DefaultProductMarginTextBox.Text, "a margem padrao de produtos", DefaultProductMarginTextBox, out var productMargin))
            {
                return false;
            }

            if (autoBackupRetention > 365)
            {
                return ExibirErroValidacao("Informe uma retencao de backup automatico entre 1 e 365 copias.", AutoBackupRetentionTextBox);
            }

            if (warrantyDays > 3650)
            {
                return ExibirErroValidacao("Informe uma garantia padrao entre 0 e 3650 dias.", DefaultWarrantyDaysTextBox);
            }

            if (productMargin > 1000m)
            {
                return ExibirErroValidacao("Informe uma margem padrao de produtos entre 0 e 1000%.", DefaultProductMarginTextBox);
            }

            configuration = new SystemConfiguration
            {
                CompanyDisplayName = businessConfiguration.CompanyDisplayName,
                CompanyLegalName = businessConfiguration.CompanyLegalName,
                CompanyDocument = businessConfiguration.CompanyDocument,
                CompanyPhone = businessConfiguration.CompanyPhone,
                CompanyWhatsApp = businessConfiguration.CompanyWhatsApp,
                CompanyAddress = businessConfiguration.CompanyAddress,
                LogoPath = businessConfiguration.LogoPath,
                ReceiptHeader = businessConfiguration.ReceiptHeader,
                ReceiptFooter = businessConfiguration.ReceiptFooter,
                PreferredTheme = ObterTagComboBox(ThemeModeComboBox, nameof(AppTheme.Light)),
                AutoBackupEnabled = AutoBackupEnabledCheckBox.IsChecked == true,
                AutoBackupRetentionCopies = autoBackupRetention,
                DefaultPrinterName = PdvPrinterComboBox.SelectedValue?.ToString() ?? _stationConfiguration.PreferredPdvPrinterName ?? string.Empty,
                OsNumberPrefix = OsNumberPrefixTextBox.Text.Trim(),
                OsNextNumber = osNextNumber,
                OrcamentoNumberPrefix = OrcamentoNumberPrefixTextBox.Text.Trim(),
                OrcamentoNextNumber = orcamentoNextNumber,
                PermissionPolicySummary = "Controle por perfis e permissoes persistidas.",
                MessageTemplateOrcamento = MessageTemplateOrcamentoTextBox.Text.Trim(),
                MessageTemplateOrdemPronta = MessageTemplateOrdemProntaTextBox.Text.Trim(),
                MessageTemplateGarantia = MessageTemplateGarantiaTextBox.Text.Trim(),
                DefaultWarrantyDays = warrantyDays,
                DefaultProductMarginPercent = productMargin,
                UpdatedBy = _funcionarioLogado.Nome
            };

            if (string.IsNullOrWhiteSpace(configuration.OsNumberPrefix))
            {
                return ExibirErroValidacao("Informe o prefixo da numeracao de OS.", OsNumberPrefixTextBox);
            }

            if (string.IsNullOrWhiteSpace(configuration.OrcamentoNumberPrefix))
            {
                return ExibirErroValidacao("Informe o prefixo da numeracao de orcamento.", OrcamentoNumberPrefixTextBox);
            }

            return true;
        }

        private void AtualizarConfiguracaoSistemaComComercial(BusinessConfiguration configuration)
        {
            _systemConfiguration.CompanyDisplayName = configuration.CompanyDisplayName;
            _systemConfiguration.CompanyLegalName = configuration.CompanyLegalName;
            _systemConfiguration.CompanyDocument = configuration.CompanyDocument;
            _systemConfiguration.CompanyPhone = configuration.CompanyPhone;
            _systemConfiguration.CompanyWhatsApp = configuration.CompanyWhatsApp;
            _systemConfiguration.CompanyAddress = configuration.CompanyAddress;
            _systemConfiguration.LogoPath = configuration.LogoPath;
            _systemConfiguration.ReceiptHeader = configuration.ReceiptHeader;
            _systemConfiguration.ReceiptFooter = configuration.ReceiptFooter;
            _systemConfiguration.UpdatedBy = _funcionarioLogado.Nome;
        }

        private bool TryGarantirPermissaoConfiguracao(string acao)
        {
            if (_permissionService.TemPermissaoCodigo("SISTEMA_CONFIGURAR"))
            {
                return true;
            }

            var mensagem = $"Alteracao bloqueada: o perfil '{_funcionarioLogado.PerfilAcesso}' nao possui SISTEMA_CONFIGURAR para {acao}.";
            _logger.LogWarning(mensagem);

            if (OperationalSettingsStatusTextBlock != null)
            {
                OperationalSettingsStatusTextBlock.Text = mensagem;
            }

            if (ReceiptPreviewTextBlock != null)
            {
                ReceiptPreviewTextBlock.Text = mensagem;
            }

            return false;
        }

        private static bool TryParsePositiveInt(string text, string fieldName, Control control, out int value)
        {
            value = 0;
            if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value <= 0)
            {
                return ExibirErroValidacao($"Informe {fieldName} valido maior que zero.", control);
            }

            return true;
        }

        private static bool TryParseNonNegativeInt(string text, string fieldName, Control control, out int value)
        {
            value = 0;
            if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < 0)
            {
                return ExibirErroValidacao($"Informe {fieldName} valida maior ou igual a zero.", control);
            }

            return true;
        }

        private static bool TryParseNonNegativeDecimal(string text, string fieldName, Control control, out decimal value)
        {
            var normalized = text.Trim();
            if ((!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value) &&
                 !decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) ||
                value < 0)
            {
                return ExibirErroValidacao($"Informe {fieldName} valida maior ou igual a zero.", control);
            }

            return true;
        }

        private static string ObterTagComboBox(ComboBox comboBox, string fallback)
        {
            return comboBox.SelectedItem is ComboBoxItem item && item.Tag != null
                ? item.Tag.ToString() ?? fallback
                : fallback;
        }

        private static void SelecionarComboBoxPorTag(ComboBox comboBox, string tag)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }

            comboBox.SelectedIndex = 0;
        }

        private void AplicarTemaPreferido(string preferredTheme)
        {
            var theme = string.Equals(preferredTheme, nameof(AppTheme.Dark), StringComparison.OrdinalIgnoreCase)
                ? AppTheme.Dark
                : AppTheme.Light;
            _themeService.ApplyTheme(theme);
        }

        private void AtualizarStatusLogo(string logoPath)
        {
            var validation = BusinessConfigurationService.ValidateLogoPath(logoPath);
            LogoStatusTextBlock.Text = validation.Message;
            LogoStatusTextBlock.Foreground = validation.IsValid
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;
        }

        private static string BuildReceiptPreview(BusinessConfiguration configuration)
        {
            var logoResumo = string.IsNullOrWhiteSpace(configuration.LogoPath)
                ? "sem logo"
                : Path.GetFileName(configuration.LogoPath);

            return
                $"{configuration.EffectiveCompanyName}\n" +
                $"{configuration.CompanyLegalName}\n" +
                $"Documento: {configuration.CompanyDocument}\n" +
                $"Telefone: {configuration.CompanyPhone}\n" +
                $"WhatsApp: {configuration.CompanyWhatsApp}\n" +
                $"Endereco: {configuration.CompanyAddress}\n" +
                $"Logo: {logoResumo}\n" +
                $"Cabecalho: {configuration.ReceiptHeader}\n" +
                $"Rodape: {configuration.ReceiptFooter}";
        }

        private bool TryValidarBackupParaRestauracao(out string caminhoBackup)
        {
            caminhoBackup = RestoreBackupPathTextBox.Text.Trim();
            var runtimeIsSqlServer = string.Equals(App.Database.RuntimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
            var extensaoEsperada = runtimeIsSqlServer ? ".bak" : ".db";

            if (string.IsNullOrWhiteSpace(caminhoBackup))
            {
                RestoreBackupStatusTextBlock.Text = $"Informe o caminho de um backup {extensaoEsperada} antes de validar/restaurar.";
                RestoreBackupStatusTextBlock.Foreground = System.Windows.Media.Brushes.DarkOrange;
                return false;
            }

            try
            {
                caminhoBackup = Path.GetFullPath(caminhoBackup);
            }
            catch (Exception ex)
            {
                RestoreBackupStatusTextBlock.Text = $"Caminho de backup invalido: {ex.Message}";
                RestoreBackupStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return false;
            }

            if (!string.Equals(Path.GetExtension(caminhoBackup), extensaoEsperada, StringComparison.OrdinalIgnoreCase))
            {
                RestoreBackupStatusTextBlock.Text = runtimeIsSqlServer
                    ? "O arquivo de restauracao precisa ser um backup SQL Server com extensao .bak."
                    : "O arquivo de restauracao precisa ser um backup SQLite com extensao .db.";
                RestoreBackupStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return false;
            }

            if (!File.Exists(caminhoBackup))
            {
                RestoreBackupStatusTextBlock.Text = $"Backup nao encontrado: {caminhoBackup}";
                RestoreBackupStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                return false;
            }

            if (!runtimeIsSqlServer)
            {
                var bancoAtual = Path.GetFullPath(App.Database.DatabasePath);
                if (string.Equals(caminhoBackup, bancoAtual, StringComparison.OrdinalIgnoreCase))
                {
                    RestoreBackupStatusTextBlock.Text = "O arquivo informado e o banco atual. Informe um backup separado para restauracao.";
                    RestoreBackupStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return false;
                }
            }

            var verificado = App.Backups.VerificarBackup(caminhoBackup);
            RestoreBackupStatusTextBlock.Text = verificado
                ? $"Backup valido e pronto para restauracao controlada: {caminhoBackup}"
                : $"Backup encontrado, mas a verificacao de integridade falhou: {caminhoBackup}";
            RestoreBackupStatusTextBlock.Foreground = verificado
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;

            App.Audit.Registrar(
                categoria: "Sistema",
                acao: "ValidarBackupRestauracao",
                entidade: "Backup",
                entidadeId: Path.GetFileName(caminhoBackup),
                detalhes: $"Verificado={verificado};Caminho={caminhoBackup}");

            return verificado;
        }

        private static bool ExibirErroValidacao(string mensagem, Control campo)
        {
            MessageBox.Show(mensagem, "Validacao", MessageBoxButton.OK, MessageBoxImage.Warning);
            campo.Focus();
            return false;
        }

        private void TestarPastaRedeButton_Click(object sender, RoutedEventArgs e)
        {
            var pasta = NetworkBackupFolderTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(pasta))
            {
                MessageBox.Show(
                    "Informe o caminho da pasta de rede para testar a conexao.",
                    "Pasta de rede",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(pasta);
                var arquivoTeste = Path.Combine(pasta, "test-write-permission.tmp");
                File.WriteAllText(arquivoTeste, "test");
                File.Delete(arquivoTeste);

                NetworkBackupStatusTextBlock.Text = $"Conexao com pasta de rede bem-sucedida: {pasta}";
                NetworkBackupStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;

                _logger.LogInfo($"Teste de conexao com pasta de rede bem-sucedido: {pasta}");
                App.Audit.Registrar("Sistema", "TestarPastaRede", "Backup", pasta, "Sucesso=true");
            }
            catch (Exception ex)
            {
                NetworkBackupStatusTextBlock.Text = $"Falha na conexao com pasta de rede: {ex.Message}";
                NetworkBackupStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;

                _logger.LogError($"Falha no teste de conexao com pasta de rede '{pasta}'.", ex);
                App.Audit.RegistrarErro("Sistema", "FalhaTestePastaRede", ex);
            }
        }

        private void CarregarInformacoesBackupButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarInformacoesBackup();
        }

        private void CarregarInformacoesBackup()
        {
            try
            {
                var backupDir = App.RuntimeBackupDirectory;
                if (!Directory.Exists(backupDir))
                {
                    UltimoBackupTextBlock.Text = "N/A";
                    StatusUltimoBackupTextBlock.Text = "Pasta nao encontrada";
                    TamanhoUltimoBackupTextBlock.Text = "N/A";
                    QuantidadeBackupsTextBlock.Text = "0";
                    return;
                }

                var backups = Directory
                    .EnumerateFiles(backupDir, "PrimoAutoEletrica_Backup_*.db")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                QuantidadeBackupsTextBlock.Text = backups.Count.ToString();

                if (backups.Any())
                {
                    var ultimoBackup = backups.First();
                    var manifesto = App.Backups.LerManifesto(ultimoBackup.FullName);

                    UltimoBackupTextBlock.Text = ultimoBackup.CreationTime.ToString("dd/MM/yyyy HH:mm:ss");
                    TamanhoUltimoBackupTextBlock.Text = FormatFileSize(ultimoBackup.Length);

                    if (manifesto != null)
                    {
                        StatusUltimoBackupTextBlock.Text = manifesto.ResultadoVerificacao;
                    }
                    else
                    {
                        var verificado = App.Backups.VerificarBackup(ultimoBackup.FullName);
                        StatusUltimoBackupTextBlock.Text = verificado ? "Verificado" : "Nao verificado";
                    }
                }
                else
                {
                    UltimoBackupTextBlock.Text = "N/A";
                    StatusUltimoBackupTextBlock.Text = "Nenhum backup";
                    TamanhoUltimoBackupTextBlock.Text = "N/A";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao carregar informacoes de backup.", ex);
                UltimoBackupTextBlock.Text = "Erro";
                StatusUltimoBackupTextBlock.Text = ex.Message;
                TamanhoUltimoBackupTextBlock.Text = "N/A";
                QuantidadeBackupsTextBlock.Text = "Erro";
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        private void TestarConexaoSqlServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var server = SqlServerTextBox.Text.Trim();
                var instance = SqlServerInstanceTextBox.Text.Trim();
                var database = SqlServerDatabaseTextBox.Text.Trim();
                var useWindowsAuth = WindowsAuthRadioButton.IsChecked == true;
                var username = SqlServerUserTextBox.Text.Trim();
                var password = SqlServerPasswordBox.Password;
                var timeout = 30;

                if (int.TryParse(SqlServerTimeoutTextBox.Text.Trim(), out var parsedTimeout))
                {
                    timeout = parsedTimeout;
                }

                if (string.IsNullOrWhiteSpace(server))
                {
                    SqlServerStatusTextBlock.Text = "Erro: Servidor e obrigatorio.";
                    SqlServerStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (string.IsNullOrWhiteSpace(database))
                {
                    SqlServerStatusTextBlock.Text = "Erro: Nome do banco e obrigatorio.";
                    SqlServerStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                if (!useWindowsAuth && (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)))
                {
                    SqlServerStatusTextBlock.Text = "Erro: Usuario e senha sao obrigatorios para autenticacao SQL Server.";
                    SqlServerStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    return;
                }

                var provider = new Services.DatabaseProviders.SqlServerDatabaseProvider(
                    server,
                    instance,
                    database,
                    useWindowsAuth,
                    username,
                    password,
                    timeout,
                    encryptConnection: true,
                    trustServerCertificate: false,
                    logger: _logger);

                var result = provider.TestConnection();

                if (result.Success)
                {
                    SqlServerStatusTextBlock.Text = $"Sucesso: {result.Message} Latencia: {result.Latency?.TotalMilliseconds:F0}ms. Versao: {result.ServerVersion}";
                    SqlServerStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    _logger.LogInfo($"Teste de conexao SQL Server bem-sucedido: {result.Message}");
                }
                else
                {
                    SqlServerStatusTextBlock.Text = $"Falha: {result.ErrorMessage}";
                    SqlServerStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    _logger.LogError($"Teste de conexao SQL Server falhou: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                SqlServerStatusTextBlock.Text = $"Erro inesperado: {ex.Message}";
                SqlServerStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                _logger.LogError("Erro ao testar conexao SQL Server.", ex);
            }
        }

        private void AtualizarInformacoesBancoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var runtimeIsSQLite = string.Equals(App.Database.RuntimeProvider, "SQLite", StringComparison.OrdinalIgnoreCase);
                CurrentDatabaseTypeTextBlock.Text = runtimeIsSQLite && !App.Database.IsUsingUnsupportedProviderFallback
                    ? "SQLite Local"
                    : runtimeIsSQLite
                        ? $"SQLite Local (runtime) / configurado: {_settings.Provider}"
                        : "SQL Server";
                CurrentServerTextBlock.Text = runtimeIsSQLite ? "Local" : $"{_settings.SqlServerHost}\\{_settings.SqlServerInstance}";
                CurrentDatabaseNameTextBlock.Text = runtimeIsSQLite ? Path.GetFileName(App.Database.DatabasePath) : _settings.SqlServerDatabase;

                using var connection = App.Database.GetConnection();
                connection.Open();

                var versionCommand = connection.CreateCommand();
                versionCommand.CommandText = runtimeIsSQLite ? "SELECT sqlite_version();" : "SELECT @@VERSION;";
                var version = versionCommand.ExecuteScalar()?.ToString() ?? "Unknown";

                CurrentDatabaseVersionTextBlock.Text = App.Database.IsUsingUnsupportedProviderFallback
                    ? $"{version} (fallback SQLite explicito)"
                    : version;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao atualizar informacoes do banco.", ex);
                CurrentDatabaseTypeTextBlock.Text = "Erro";
                CurrentServerTextBlock.Text = ex.Message;
                CurrentDatabaseNameTextBlock.Text = "N/A";
                CurrentDatabaseVersionTextBlock.Text = "N/A";
            }
        }

        private void CarregarConfiguracoesMultiusuario()
        {
            try
            {
                MachineNameTextBox.Text = _stationConfiguration.MachineName;
                StationNameTextBox.Text = _stationConfiguration.StationName;
                StationDescriptionTextBox.Text = _stationConfiguration.Description ?? string.Empty;

                // Selecionar tipo de estação
                foreach (ComboBoxItem item in StationTypeComboBox.Items)
                {
                    if (item.Tag?.ToString() == _stationConfiguration.StationType.ToString())
                    {
                        StationTypeComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Carregar informações da estação atual
                CurrentMachineNameTextBlock.Text = _stationConfiguration.MachineName;
                CurrentStationNameTextBlock.Text = _stationConfiguration.StationName;
                CurrentStationTypeTextBlock.Text = _stationConfiguration.StationType.ToString();
                StationConfiguredAtTextBlock.Text = _stationConfiguration.ConfiguredAt.ToString("dd/MM/yyyy HH:mm");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao carregar configuracoes de multiusuario: {ex.Message}");
            }
        }

        private void SalvarConfiguracoesMultiusuario()
        {
            try
            {
                var stationName = StationNameTextBox.Text.Trim();
                var stationDescription = StationDescriptionTextBox.Text.Trim();

                StationType stationType = StationType.Administrativo;
                if (StationTypeComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                {
                    Enum.TryParse(selectedItem.Tag.ToString(), out stationType);
                }

                _stationConfiguration.StationName = string.IsNullOrWhiteSpace(stationName) ? _stationConfiguration.MachineName : stationName;
                _stationConfiguration.StationType = stationType;
                _stationConfiguration.Description = string.IsNullOrWhiteSpace(stationDescription) ? null : stationDescription;
                _stationConfiguration.UseConfiguredPdvPrinter = UseConfiguredPdvPrinterCheckBox.IsChecked == true;
                _stationConfiguration.PreferredPdvPrinterName = PdvPrinterComboBox.SelectedValue?.ToString();

                StationService.SaveConfiguration(App.RuntimeAppDataPath, _stationConfiguration);

                _logger.LogInfo($"Configuracoes de estacao salvas: {_stationConfiguration.StationName} ({_stationConfiguration.StationType})");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao salvar configuracoes de multiusuario: {ex.Message}");
            }
        }

        private void CarregarConfiguracoesImpressaoPdv()
        {
            try
            {
                var snapshot = _printerDiagnosticsService.CaptureSnapshot();
                PdvPrinterComboBox.ItemsSource = snapshot.Printers;
                UseConfiguredPdvPrinterCheckBox.IsChecked = _stationConfiguration.UseConfiguredPdvPrinter;

                if (!string.IsNullOrWhiteSpace(_stationConfiguration.PreferredPdvPrinterName))
                {
                    var impressoraConfigurada = snapshot.Printers.FirstOrDefault(printer =>
                        string.Equals(printer.Name, _stationConfiguration.PreferredPdvPrinterName, StringComparison.OrdinalIgnoreCase));
                    if (impressoraConfigurada != null)
                    {
                        PdvPrinterComboBox.SelectedItem = impressoraConfigurada;
                    }
                }

                if (PdvPrinterComboBox.SelectedItem == null)
                {
                    PdvPrinterComboBox.SelectedItem = snapshot.DefaultPrinter ?? snapshot.Printers.FirstOrDefault();
                }

                PdvPrinterStatusTextBlock.Text = snapshot.BuildSummary();
            }
            catch (Exception ex)
            {
                PdvPrinterStatusTextBlock.Text = $"Falha ao carregar impressoras: {ex.Message}";
                _logger.LogWarning($"Falha ao carregar impressoras do PDV: {ex.Message}");
            }
        }

        private void AtualizarImpressorasButton_Click(object sender, RoutedEventArgs e)
        {
            CarregarConfiguracoesImpressaoPdv();
        }

        private void SalvarConfiguracoesEstacaoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryGarantirPermissaoConfiguracao("salvar estacao e impressora padrao"))
            {
                return;
            }

            var impressoraAnterior = _stationConfiguration.PreferredPdvPrinterName ?? "Nao configurada";
            var usarAnterior = _stationConfiguration.UseConfiguredPdvPrinter;

            SalvarConfiguracoesMultiusuario();
            CarregarConfiguracoesMultiusuario();

            var impressoraAtual = _stationConfiguration.PreferredPdvPrinterName ?? "Nao configurada";
            _systemConfiguration.DefaultPrinterName = impressoraAtual;
            _systemConfiguration.UpdatedBy = _funcionarioLogado.Nome;
            _systemConfigurationService.SaveAuthorized(_systemConfiguration, _funcionarioLogado, _permissionService);

            PdvPrinterStatusTextBlock.Text =
                $"Configuracao de estacao/impressao salva em {DateTime.Now:dd/MM/yyyy HH:mm}. " +
                $"Usar preferencial={_stationConfiguration.UseConfiguredPdvPrinter}; Impressora={impressoraAtual}.";

            App.Audit.Registrar(
                categoria: "Sistema",
                acao: "AtualizarConfiguracaoEstacao",
                entidade: "Estacao",
                entidadeId: _stationConfiguration.MachineName,
                detalhes: $"Estacao={_stationConfiguration.StationName}; Tipo={_stationConfiguration.StationType}; UseConfiguredPdvPrinter={_stationConfiguration.UseConfiguredPdvPrinter}; PreferredPdvPrinter={impressoraAtual}",
                valorAnterior: $"UseConfiguredPdvPrinter={usarAnterior}; PreferredPdvPrinter={impressoraAnterior}",
                valorNovo: $"UseConfiguredPdvPrinter={_stationConfiguration.UseConfiguredPdvPrinter}; PreferredPdvPrinter={impressoraAtual}");
        }
    }
}
