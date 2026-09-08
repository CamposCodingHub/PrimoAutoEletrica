using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Views;
using PrimoAutoEletrica.DependencyInjection;
using PrimoAutoEletrica.Simulation;

namespace PrimoAutoEletrica
{
    public partial class App : Application
    {
        public string ClienteTelefone { get; set; } = string.Empty;
        public new static App Current => (App)Application.Current;
        public static IServiceProvider Services { get; private set; } = null!;
        
        private static readonly object InfrastructureLock = new();

        private static AppRuntimeConfiguration _runtimeConfiguration = AppRuntimeConfiguration.CreateDefault();
        private static bool _infrastructureInitialized;
        private static bool _infrastructureInitFailed;
        private static Exception? _infrastructureInitException;
        private static LoggerService? _logger;
        private static AppSessionService? _session;
        private static DatabaseService? _database;
        private static RepositoryRegistry? _repositories;
        private static AuditLogService? _audit;
        private static DatabaseBackupService? _backups;
        private static RegistroBloqueioService? _locks;
        private static DatabaseHealthService? _databaseHealth;
        private static SynchronizationService? _synchronizationService;
        private static LocalSyncService? _localSyncService;

        // Proteção contra recursão infinita no tratamento de exceções
        private static bool _isHandlingGlobalException = false;

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

        /// <summary>
        /// True quando o AppData atual e modo automatizado e esta fora da arvore de producao
        /// (%LOCALAPPDATA%\PrimoAutoEletrica). Aceita AutomatedTests padrao ou --app-data explicito.
        /// </summary>
        public static bool IsIsolatedAutomatedAppData
        {
            get
            {
                if (!IsAutomatedTestMode)
                {
                    return false;
                }

                var appData = RuntimeAppDataPath;
                if (string.IsNullOrWhiteSpace(appData))
                {
                    return false;
                }

                var productionRoot = Path.GetFullPath(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica"));
                var resolved = Path.GetFullPath(appData);
                if (string.Equals(resolved, productionRoot, StringComparison.OrdinalIgnoreCase) ||
                    resolved.StartsWith(productionRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return true;
            }
        }

        public static string RuntimeModeName => _runtimeConfiguration.ModeName;
        public static string RuntimeAppDataPath => _runtimeConfiguration.AppDataPath;
        public static string RuntimeLogDirectory => _runtimeConfiguration.LogDirectory;
        public static string RuntimeBackupDirectory => _runtimeConfiguration.BackupDirectory;
        public static string RuntimeSmokeFilter => _runtimeConfiguration.SmokeFilter;

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureRuntime(AppRuntimeConfiguration.FromArgs(e.Args));
            Services = ConfigureServices();
            
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
                ExecutarBackupAutomaticoInicial();

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
                try
                {
                    _logger?.LogCritical("Falha critica ao iniciar a aplicacao.", ex);
                }
                catch
                {
                    LogCriticalErrorToFallbackFile("Falha critica ao iniciar a aplicacao.", ex);
                }

                if (!IsAutomatedTestMode)
                {
                    MessageBox.Show(
                        $"Erro critico ao iniciar o sistema:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                        "Erro de Inicializacao",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }

                Shutdown(1);
            }
        }

        private void ExecutarBackupAutomaticoInicial()
        {
            if (string.Equals(Database.RuntimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                Audit.RegistrarSistema("BackupAutomatico", "Backup automatico por arquivo ignorado porque o runtime atual e SQL Server.");
                return;
            }

            var configuration = CarregarConfiguracaoSistemaSegura();
            if (configuration?.AutoBackupEnabled == false)
            {
                Audit.RegistrarSistema("BackupAutomatico", "Backup automatico diario desativado nas configuracoes do sistema.");
                return;
            }

            // Inicia o timer de backup automático diário
            Backups.IniciarBackupAutomatico(configuration?.AutoBackupRetentionCopies ?? 10);
        }

        private void ExecutarBackupAutomaticoEncerramento()
        {
            if (_database != null && string.Equals(_database.RuntimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                _audit?.RegistrarSistema("BackupEncerramento", "Backup de encerramento por arquivo ignorado porque o runtime atual e SQL Server.");
                return;
            }

            var configuration = CarregarConfiguracaoSistemaSegura();
            if (configuration?.AutoBackupEnabled == false)
            {
                _audit?.RegistrarSistema("BackupEncerramento", "Backup automatico de encerramento desativado nas configuracoes do sistema.");
                return;
            }

            // Para o timer de backup automático e executa backup de encerramento
            _backups?.ExecutarBackupAoEncerrar(configuration?.AutoBackupRetentionCopies ?? 20);
            _audit?.RegistrarSistema("BackupEncerramento", "Backup automatico de encerramento executado e timer parado");
        }

        private SystemConfiguration? CarregarConfiguracaoSistemaSegura()
        {
            try
            {
                return _database == null
                    ? null
                    : new SystemConfigurationService(_database, _logger).LoadOrCreate(RuntimeAppDataPath);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Falha ao carregar configuracoes do sistema para backup automatico: {ex.Message}");
                return null;
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

                    ExecutarBackupAutomaticoEncerramento();

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





        

        private static void LogCriticalErrorToFallbackFile(string message, Exception exception)
        {
            try
            {
                var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica", "Logs");
                Directory.CreateDirectory(logDirectory);

                var logPath = Path.Combine(logDirectory, "CriticalErrors.log");
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\nException: {exception}\nStack Trace: {exception.StackTrace}\n\n";

                File.AppendAllText(logPath, logMessage);
            }
            catch
            {
                // Se até o fallback falhar, não fazer nada para evitar crash
            }
        }

        private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            // Proteção contra recursão infinita
            if (_isHandlingGlobalException)
            {
                return;
            }

            _isHandlingGlobalException = true;

            try
            {
                if (e.ExceptionObject is Exception exception)
                {
                    try
                    {
                        Logger.LogCritical("Excecao nao tratada no dominio da aplicacao.", exception);
                    }
                    catch (Exception loggerEx)
                    {
                        LogCriticalErrorToFallbackFile("Logger falhou ao registrar exceção de domínio", loggerEx);
                    }

                    try
                    {
                        Audit.RegistrarErro("Aplicacao", "ExcecaoDominio", exception, criticidade: "Critical");
                    }
                    catch (Exception auditEx)
                    {
                        LogCriticalErrorToFallbackFile("AuditLog falhou ao registrar exceção de domínio", auditEx);
                    }
                    return;
                }

                try
                {
                    Logger.LogCritical("Falha nao tratada no dominio da aplicacao sem objeto Exception.");
                }
                catch (Exception loggerEx)
                {
                    LogCriticalErrorToFallbackFile("Logger falhou ao registrar falha de domínio sem Exception", loggerEx);
                }
                // Nenhuma exceção disponível neste contexto; registrar aviso genérico
                _logger?.LogWarning("Falha ao executar rotinas de encerramento da aplicacao sem exceção disponível.");
                // Registrar erro sem exceção concreta
                _audit?.RegistrarErro("Aplicacao", "FalhaEncerramento", null, criticidade: "Warning");
            }
            finally
            {
                try
                {
                    _localSyncService?.Dispose();
                }
                catch { }
                // base.OnExit(e); // Removed to avoid duplicate exit handling
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
            // Proteção contra recursão infinita
            if (_isHandlingGlobalException)
            {
                // Se já estamos tratando uma exceção global, não fazer nada para evitar stack overflow
                e.Handled = true;
                return;
            }

            _isHandlingGlobalException = true;

            try
            {
                // Usar campos ja inicializados — nao chamar Logger/Audit (reentrada/deadlock no init).
                try
                {
                    _logger?.LogError("Excecao nao tratada na thread principal da interface.", e.Exception);
                }
                catch (Exception loggerEx)
                {
                    LogCriticalErrorToFallbackFile("Logger falhou ao registrar exceção não tratada", loggerEx);
                }

                try
                {
                    _audit?.RegistrarErro("UI", "ExcecaoNaoTratada", e.Exception);
                }
                catch (Exception auditEx)
                {
                    LogCriticalErrorToFallbackFile("AuditLog falhou ao registrar exceção não tratada", auditEx);
                }

                if (_logger == null)
                {
                    LogCriticalErrorToFallbackFile("Excecao nao tratada (infraestrutura indisponivel)", e.Exception);
                }

                ErrorHandlingService.HandleException(
                    "UI",
                    "ExcecaoNaoTratada",
                    e.Exception,
                    "O sistema encontrou um erro inesperado nesta operacao. O evento foi registrado em log e a aplicacao tentara continuar.",
                    showWindow: !IsAutomatedTestMode);

                if (IsAutomatedTestMode)
                {
                    e.Handled = true;
                    return;
                }

                e.Handled = true;
                return;

            }
            finally
            {
                _isHandlingGlobalException = false;
            }
        }

#if false
// Duplicate LogCriticalErrorToFallbackFile method disabled to avoid duplicate definition
#endif



        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // Proteção contra recursão infinita
            if (_isHandlingGlobalException)
            {
                e.SetObserved();
                return;
            }

            _isHandlingGlobalException = true;
            try
            {
                try
                {
                    _logger?.LogError("Excecao nao observada em tarefa asincrona.", e.Exception);
                }
                catch (Exception loggerEx)
                {
                    LogCriticalErrorToFallbackFile("Logger falhou ao registrar exceção de task não observada", loggerEx);
                }

                try
                {
                    _audit?.RegistrarErro("Task", "ExcecaoNaoObservada", e.Exception);
                }
                catch (Exception auditEx)
                {
                    LogCriticalErrorToFallbackFile("AuditLog falhou ao registrar exceção de task não observada", auditEx);
                }

                if (_logger == null)
                {
                    LogCriticalErrorToFallbackFile("Excecao de task (infraestrutura indisponivel)", e.Exception);
                }
            }
            finally
            {
                _isHandlingGlobalException = false;
            }
        }
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register core services via extension
            services.AddPrimoAutoEletricaCore();
            
            // Register repositories via extension
            services.AddPrimoAutoEletricaRepositories();

            // Register ViewModels via extension
            services.AddPrimoAutoEletricaViewModels();

            // Register logging configuration
            services.AddPrimoAutoEletricaLogging();

            // Register UI services
            services.AddSingleton<ThemeService>();

            return services.BuildServiceProvider();
        }

        // Inicializa a infraestrutura da aplicação se ainda não foi inicializada
        private static void EnsureInfrastructureInitialized()
        {
            if (_infrastructureInitialized) return;
            if (_infrastructureInitFailed)
            {
                throw new InvalidOperationException(
                    "Infraestrutura da aplicacao falhou na inicializacao.",
                    _infrastructureInitException);
            }

            lock (InfrastructureLock)
            {
                if (_infrastructureInitialized) return;
                if (_infrastructureInitFailed)
                {
                    throw new InvalidOperationException(
                        "Infraestrutura da aplicacao falhou na inicializacao.",
                        _infrastructureInitException);
                }

                try
                {
                    Services = ConfigureServices();
                    _logger = Services.GetRequiredService<LoggerService>();
                    _session = Services.GetRequiredService<AppSessionService>();
                    _database = Services.GetRequiredService<DatabaseService>();
                    _repositories = Services.GetRequiredService<RepositoryRegistry>();
                    _audit = Services.GetRequiredService<AuditLogService>();
                    _backups = Services.GetRequiredService<DatabaseBackupService>();
                    _locks = Services.GetRequiredService<RegistroBloqueioService>();
                    _databaseHealth = Services.GetRequiredService<DatabaseHealthService>();
                    _synchronizationService = Services.GetRequiredService<SynchronizationService>();
                    _localSyncService = Services.GetRequiredService<LocalSyncService>();

                    _infrastructureInitialized = true;
                }
                catch (Exception ex)
                {
                    _infrastructureInitFailed = true;
                    _infrastructureInitException = ex;
                    _logger ??= new LoggerService(
                        Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "PrimoAutoEletrica",
                            "Logs"));
                    try { _logger.LogCritical("Falha ao inicializar infraestrutura.", ex); } catch { }
                    throw;
                }
            }
        }

        // Configura a runtime da aplicação com base na configuração fornecida
        private static void ConfigureRuntime(AppRuntimeConfiguration config)
        {
            // Aplica a configuração de runtime ao objeto estático
            _runtimeConfiguration = config ?? AppRuntimeConfiguration.CreateDefault();
            // Atualiza propriedades de conveniência usadas em toda a aplicação
            // Runtime configuration properties are read-only; no reassignment needed

        }
        
    }

}
