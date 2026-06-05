using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Views;

namespace PrimoAutoEletrica
{
    public partial class App : Application
    {
        private static readonly object InfrastructureLock = new();

        private static AppRuntimeConfiguration _runtimeConfiguration = AppRuntimeConfiguration.CreateDefault();
        private static bool _infrastructureInitialized;
        private static LoggerService? _logger;
        private static AppSessionService? _session;
        private static DatabaseService? _database;
        private static RepositoryRegistry? _repositories;
        private static AuditLogService? _audit;
        private static DatabaseBackupService? _backups;
        private static RegistroBloqueioService? _locks;
        private static DatabaseHealthService? _databaseHealth;
            private static LocalSyncService? _localSyncService;

        public static LoggerService Logger
        {
            get
            {
                EnsureInfrastructureInitialized();
                return _logger!;
            }
        }

        public static AppSessionService Session
        {
            get
            {
                EnsureInfrastructureInitialized();
                return _session!;
            }
        }

        public static DatabaseService Database
        {
            get
            {
                EnsureInfrastructureInitialized();
                return _database!;
            }
        }

        public static RepositoryRegistry Repositories
        {
            get
            {
                EnsureInfrastructureInitialized();
                return _repositories!;
            }
        }

        public static AuditLogService Audit
        {
            get
            {
                EnsureInfrastructureInitialized();
                return _audit!;
            }
        }

        public static DatabaseBackupService Backups
        {
            get
            {
                EnsureInfrastructureInitialized();
                return _backups!;
            }
        }

        public static RegistroBloqueioService Locks
        {
            get
            {
                EnsureInfrastructureInitialized();
                return _locks!;
            }
        }

        public static DatabaseHealthService DatabaseHealth
        {
            get
            {
                EnsureInfrastructureInitialized();
                return _databaseHealth!;
            }
        }

        public static void SendLocalSyncMessage(string message)
        {
            try
            {
                _localSyncService?.Send(message);
            }
            catch (Exception ex)
            {
                try { Logger.LogWarning($"Falha ao enviar mensagem LocalSync: {ex.Message}"); } catch { }
            }
        }

        public static bool IsSmokeTestMode => _runtimeConfiguration.IsSmokeTestMode;
        public static bool IsWorkflowTestMode => _runtimeConfiguration.IsWorkflowTestMode;
        public static bool IsAutomatedTestMode => _runtimeConfiguration.IsAutomatedTestMode;
        public static string RuntimeModeName => _runtimeConfiguration.ModeName;
        public static string RuntimeAppDataPath => _runtimeConfiguration.AppDataPath;
        public static string RuntimeLogDirectory => _runtimeConfiguration.LogDirectory;
        public static string RuntimeBackupDirectory => _runtimeConfiguration.BackupDirectory;
        public static string RuntimeSmokeFilter => _runtimeConfiguration.SmokeFilter;

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureRuntime(AppRuntimeConfiguration.FromArgs(e.Args));
            // Aplicar tema salvo antes de abrir qualquer janela
            var themeService = new ThemeService();
            themeService.ApplyTheme(themeService.GetCurrentTheme());

            base.OnStartup(e);

            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler(OnAnyWindowLoadedForLayout));

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            try
            {
                Logger.LogInfo("Inicializacao do ERP iniciada.");
                Session.EndSession();
                Audit.RegistrarSistema("Startup", "Inicializacao do ERP iniciada.");
                DatabaseHealth.Verificar();
                Backups.CriarBackupAutomaticoDiario();

                if (IsSmokeTestMode)
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    ExecuteSmokeTestAndShutdown();
                    return;
                }

                if (IsWorkflowTestMode)
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    ExecuteWorkflowTestAndShutdown();
                    return;
                }

                var login = new LoginWindow();
                Current.MainWindow = login;
                login.Show();

                Logger.LogInfo("Tela de login carregada com sucesso.");
            }
            catch (Exception ex)
            {
                Logger.LogCritical("Falha critica ao iniciar a aplicacao.", ex);

                MessageBox.Show(
                    $"Erro critico ao iniciar o sistema:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Erro de Inicializacao",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_infrastructureInitialized && !IsAutomatedTestMode)
                {
                    var usuario = _session?.IsAuthenticated == true ? _session.UserName : "Nao autenticado";
                    _audit?.RegistrarSistema("Shutdown", $"Encerramento do ERP iniciado. Usuario={usuario}");

                    var caminhoBackup = _backups?.CriarBackupAoEncerrar();
                    if (!string.IsNullOrWhiteSpace(caminhoBackup))
                    {
                        _audit?.RegistrarSistema("BackupEncerramento", $"Backup automatico de encerramento criado em {caminhoBackup}");
                    }

                    if (_session?.IsAuthenticated == true && _database != null && _logger != null)
                    {
                        new UserSessionService(_database, _logger, _session).EndSession();
                    }

                    _session?.EndSession();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Falha ao executar rotinas de encerramento da aplicacao.", ex);
                _audit?.RegistrarErro("Aplicacao", "FalhaEncerramento", ex, criticidade: "Warning");
            }
            finally
            {
                try
                {
                    _localSyncService?.Dispose();
                }
                catch { }
                base.OnExit(e);
            }
        }

        private static void ConfigureRuntime(AppRuntimeConfiguration runtimeConfiguration)
        {
            lock (InfrastructureLock)
            {
                if (_infrastructureInitialized)
                {
                    return;
                }

                _runtimeConfiguration = runtimeConfiguration;
            }
        }

        private static void EnsureInfrastructureInitialized()
        {
            if (_infrastructureInitialized)
            {
                return;
            }

            lock (InfrastructureLock)
            {
                if (_infrastructureInitialized)
                {
                    return;
                }

                EnsureDirectoryStructure();

                _logger = new LoggerService(_runtimeConfiguration.LogDirectory);
                _session = new AppSessionService();
                _database = new DatabaseService(_runtimeConfiguration.AppDataPath, logger: _logger);
                _repositories = new RepositoryRegistry(_database, _logger);
                _audit = new AuditLogService(_database, _logger, _session);
                var networkBackupDir = DatabaseConnectionSettingsService.LoadOrCreateDefault(_runtimeConfiguration.AppDataPath, _logger).NetworkBackupDirectory;
                _backups = new DatabaseBackupService(_database, _logger, _runtimeConfiguration.BackupDirectory, networkBackupDir);
                _locks = new RegistroBloqueioService(_database, _session);
                _databaseHealth = new DatabaseHealthService(_database, _logger);
                // Inicializar servico de sincronizacao local se solicitado nas configuracoes da estacao
                try
                {
                    var stationConfig = StationService.GetConfiguration(_runtimeConfiguration.AppDataPath);
                    if (stationConfig.UseLocalSync)
                    {
                        _localSyncService = new LocalSyncService(stationConfig.LocalSyncPort);
                        var syncHandler = new LocalSyncMessageHandler(_logger);
                        _localSyncService.MessageReceived += (msg, ep) => syncHandler.Handle(msg, ep);
                        _localSyncService.Start();
                        _logger.LogInfo($"Local sync service started on port {stationConfig.LocalSyncPort}.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Falha ao iniciar LocalSyncService: {ex.Message}");
                }
                _infrastructureInitialized = true;
            }
        }

        private static void EnsureDirectoryStructure()
        {
            var appDataPath = _runtimeConfiguration.AppDataPath;

            var directories = new[]
            {
                appDataPath,
                Path.Combine(appDataPath, "Backups"),
                Path.Combine(appDataPath, "Logs"),
                Path.Combine(appDataPath, "Database"),
                Path.Combine(appDataPath, "Temp"),
                Path.Combine(appDataPath, "Reports"),
                Path.Combine(appDataPath, "Config"),
                Path.Combine(appDataPath, "Updates"),
                Path.Combine(appDataPath, "Exports"),
                Path.Combine(appDataPath, "Imports"),
                Path.Combine(appDataPath, "Media"),
                Path.Combine(appDataPath, "Media", "Produtos"),
                Path.Combine(appDataPath, "Media", "Clientes")
            };

            foreach (var directory in directories)
            {
                try
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Falha ao criar diretório '{directory}': {ex.Message}", ex);
                }
            }
        }

        private void ExecuteSmokeTestAndShutdown()
        {
            try
            {
                var smokeTestService = new UiSmokeTestService(Logger, RuntimeSmokeFilter);
                var result = smokeTestService.Run();
                var summary = $"Smoke test UI finalizado. Total={result.TotalChecks}, Sucesso={result.PassedChecks}, Falhas={result.FailedChecks}, Relatorio={result.ReportPath}";

                Logger.LogInfo(summary);
                Audit.RegistrarSistema("SmokeTestUi", summary, result.HasFailures ? "Error" : "Info", !result.HasFailures);
                Shutdown(result.HasFailures ? 2 : 0);
            }
            catch (Exception ex)
            {
                Logger.LogCritical("Falha ao executar smoke test de UI.", ex);
                Audit.RegistrarErro("SmokeTestUi", "FalhaExecucao", ex, criticidade: "Critical");
                Shutdown(3);
            }
        }

        private void ExecuteWorkflowTestAndShutdown()
        {
            try
            {
                var workflowTestService = new OperationalWorkflowTestService(Logger);
                var result = workflowTestService.Run();
                var summary = $"Workflow test finalizado. Total={result.TotalChecks}, Sucesso={result.PassedChecks}, Falhas={result.FailedChecks}, Relatorio={result.ReportPath}";

                Logger.LogInfo(summary);
                Audit.RegistrarSistema("WorkflowTest", summary, result.HasFailures ? "Error" : "Info", !result.HasFailures);
                Shutdown(result.HasFailures ? 4 : 0);
            }
            catch (Exception ex)
            {
                Logger.LogCritical("Falha ao executar workflow test.", ex);
                Audit.RegistrarErro("WorkflowTest", "FalhaExecucao", ex, criticidade: "Critical");
                Shutdown(5);
            }
        }

        private static void OnAnyWindowLoadedForLayout(object sender, RoutedEventArgs e)
        {
            if (sender is not Window window || window is MainWindow)
            {
                return;
            }

            var workArea = SystemParameters.WorkArea;
            var maxWidth = Math.Max(640, workArea.Width - 48);
            var maxHeight = Math.Max(460, workArea.Height - 48);

            if (window.MinWidth > maxWidth)
            {
                window.MinWidth = maxWidth;
            }

            if (window.MinHeight > maxHeight)
            {
                window.MinHeight = maxHeight;
            }

            if (double.IsInfinity(window.MaxWidth) || window.MaxWidth > maxWidth)
            {
                window.MaxWidth = maxWidth;
            }

            if (double.IsInfinity(window.MaxHeight) || window.MaxHeight > maxHeight)
            {
                window.MaxHeight = maxHeight;
            }

            if (!double.IsNaN(window.Width) && window.Width > maxWidth)
            {
                window.Width = maxWidth;
            }

            if (!double.IsNaN(window.Height) && window.Height > maxHeight)
            {
                window.Height = maxHeight;
            }

            if (window.Owner != null && window.ResizeMode == ResizeMode.NoResize)
            {
                window.ResizeMode = ResizeMode.CanResize;
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogError("Excecao nao tratada na thread principal da interface.", e.Exception);
            Audit.RegistrarErro("UI", "ExcecaoNaoTratada", e.Exception);

            if (IsAutomatedTestMode)
            {
                e.Handled = true;
                return;
            }

            MessageBox.Show(
                "O sistema encontrou um erro inesperado nesta operacao. O evento foi registrado em log e a aplicacao tentara continuar.",
                "Erro inesperado",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            e.Handled = true;
        }

        private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                Logger.LogCritical("Excecao nao tratada no dominio da aplicacao.", exception);
                Audit.RegistrarErro("Aplicacao", "ExcecaoDominio", exception, criticidade: "Critical");
                return;
            }

            Logger.LogCritical("Falha nao tratada no dominio da aplicacao sem objeto Exception.");
            Audit.RegistrarSistema("ExcecaoDominio", "Falha nao tratada no dominio da aplicacao sem objeto Exception.", "Critical", false);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Logger.LogError("Excecao nao observada em tarefa assincrona.", e.Exception);
            Audit.RegistrarErro("Task", "ExcecaoNaoObservada", e.Exception);
            e.SetObserved();
        }
    }
}
