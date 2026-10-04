using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Fiscal;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Models;
using System.Data.Common;

namespace PrimoAutoEletrica.DependencyInjection
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddPrimoAutoEletricaCore(this IServiceCollection services)
        {
            services.AddSingleton(sp => new LoggerService(App.RuntimeLogDirectory));
            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<LoggerService>();
                return new DatabaseService(
                    appDataPathOverride: App.RuntimeAppDataPath,
                    logger: logger);
            });
            services.AddSingleton<RepositoryRegistry>();
            services.AddSingleton<AuditLogService>();
            services.AddSingleton(sp =>
            {
                var database = sp.GetRequiredService<DatabaseService>();
                var logger = sp.GetRequiredService<LoggerService>();
                return new DatabaseBackupService(
                    database,
                    logger,
                    backupDirectory: App.RuntimeBackupDirectory);
            });
            services.AddSingleton<RegistroBloqueioService>();
            services.AddSingleton<DatabaseHealthService>();
            services.AddSingleton<SynchronizationService>();
            services.AddSingleton<LocalSyncService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<ContabilExportService>();
            services.AddSingleton<MigrationService>();
            services.AddSingleton<AppSessionService>();
            services.AddSingleton<LocalizationService>();
            services.AddSingleton<LanguageManager>();
            services.AddSingleton<FilialService>();
            services.AddSingleton<PrinterDiagnosticsService>();
            services.AddSingleton<RelatorioExportService>();
            services.AddSingleton<AuditTrailService>();
            services.AddSingleton<ITwoFactorService, TwoFactorService>();
            services.AddSingleton<AppCacheService>();
            services.AddSingleton<SoftDeleteService>();

            // Fundação fiscal — Focus adapter; produção bloqueada; Fake apenas em testes.
            services.AddSingleton(sp => new FiscalConfigurationService(
                App.RuntimeAppDataPath,
                sp.GetService<LoggerService>()));
            services.AddSingleton<FocusNfeHttpClient>();
            services.AddSingleton<FiscalOperationStore>();
            services.AddSingleton<FiscalDocumentValidator>();
            services.AddSingleton<VendaFiscalNFeMapper>();
            services.AddSingleton<FiscalNFePreviewBuilder>();
            services.AddSingleton(sp => new FiscalHealthCheck(
                sp.GetRequiredService<FiscalConfigurationService>(),
                sp.GetService<RepositoryRegistry>()));
            services.AddSingleton<IFiscalProvider, FocusNfeProvider>();
            services.AddSingleton<FiscalApplicationService>();
            services.AddSingleton<NFeHomologationService>();
            services.AddSingleton<NFeEmissaoService>();
            services.AddSingleton<FiscalOperationsCenterService>();

            services.AddTransient(sp => PermissionService.CriarParaSessaoAtual(
                sp.GetService<LoggerService>(),
                sp.GetService<DatabaseService>()));
            services.AddTransient<RBACService>();
            services.AddTransient<NavigationService>();
            services.AddTransient<OrcamentoDatabaseService>();
            services.AddTransient<EstoqueOperationalService>();
            services.AddTransient<FinanceiroDatabaseService>();
            services.AddTransient<IGestaoComprasService, GestaoComprasService>();
            services.AddTransient<GestaoComprasService>();
            services.AddTransient<IFerramentaService, FerramentaService>();
            services.AddTransient<FerramentaService>();
            services.AddTransient<ITransferenciaEstoqueService, TransferenciaEstoqueService>();
            services.AddTransient<TransferenciaEstoqueService>();
            services.AddTransient<IGestaoFrotasService, GestaoFrotasService>();
            services.AddTransient<GestaoFrotasService>();
            services.AddTransient<IDviInspectionService, DviInspectionService>();
            services.AddTransient<DviInspectionService>();
            services.AddTransient<Services.AI.IWorkOrderAiBridgeService, Services.AI.WorkOrderAiBridgeService>();
            services.AddTransient<Services.AI.WorkOrderAiBridgeService>();
            services.AddTransient<ISureTrackService, SureTrackService>();
            services.AddTransient<SureTrackService>();
            services.AddTransient<IBibliotecaTecnicaService, BibliotecaTecnicaService>();
            services.AddTransient<BibliotecaTecnicaService>();
            services.AddTransient<ITroubleshootingFlowService, TroubleshootingFlowService>();
            services.AddTransient<TroubleshootingFlowService>();
            services.AddTransient<ICalculadoraQuedaTensaoService, CalculadoraQuedaTensaoService>();
            services.AddTransient<CalculadoraQuedaTensaoService>();
            services.AddSingleton<IGatewayFiscalCorporativo, GatewayFiscalCorporativoService>();
            services.AddSingleton<GatewayFiscalCorporativoService>();
            services.AddSingleton<IEtiquetaTermicaZplService, EtiquetaTermicaZplService>();
            services.AddSingleton<EtiquetaTermicaZplService>();
            services.AddSingleton(sp => new LicenseService(App.RuntimeAppDataPath, sp.GetService<LoggerService>()));
            services.AddSingleton<Services.AI.AutomotiveWebSearchService>();
            services.AddSingleton<Services.AI.AutomotiveDiagramImageService>();
            services.AddSingleton<Services.AI.AutomotiveDiagnosticRAGService>();
            services.AddSingleton<Services.AI.AIToolRegistry>(sp =>
            {
                return new Services.AI.AIToolRegistry(
                    sp.GetService<INavigationService>(),
                    sp.GetService<Repositories.IProdutoRepository>(),
                    sp.GetService<IFerramentaService>(),
                    sp.GetService<IGestaoComprasService>(),
                    sp.GetService<Repositories.IClienteRepository>(),
                    sp.GetRequiredService<Services.AI.AutomotiveDiagnosticRAGService>(),
                    sp.GetRequiredService<Services.AI.AutomotiveWebSearchService>(),
                    sp.GetService<Services.AI.IWorkOrderAiBridgeService>(),
                    sp.GetService<ISureTrackService>(),
                    sp.GetService<IBibliotecaTecnicaService>(),
                    sp.GetService<ITroubleshootingFlowService>(),
                    sp.GetService<ICalculadoraQuedaTensaoService>()
                );
            });
            services.AddSingleton<Services.AI.DeterministicFallbackAIService>(sp =>
            {
                return new Services.AI.DeterministicFallbackAIService(
                    sp.GetRequiredService<Services.AI.AIToolRegistry>(),
                    sp.GetRequiredService<Services.AI.AutomotiveDiagnosticRAGService>(),
                    sp.GetRequiredService<Services.AI.AutomotiveWebSearchService>(),
                    sp.GetRequiredService<Services.AI.AutomotiveDiagramImageService>(),
                    sp.GetService<ISureTrackService>(),
                    sp.GetService<IBibliotecaTecnicaService>(),
                    sp.GetService<ITroubleshootingFlowService>(),
                    sp.GetService<ICalculadoraQuedaTensaoService>()
                );
            });
            services.AddSingleton<Services.AI.LocalOllamaAIService>(sp =>
            {
                var fallbackService = sp.GetRequiredService<Services.AI.DeterministicFallbackAIService>();
                var toolRegistry = sp.GetRequiredService<Services.AI.AIToolRegistry>();
                var ragService = sp.GetRequiredService<Services.AI.AutomotiveDiagnosticRAGService>();
                var webSearchService = sp.GetRequiredService<Services.AI.AutomotiveWebSearchService>();
                return new Services.AI.LocalOllamaAIService(fallbackService, toolRegistry, ragService, webSearchService: webSearchService);
            });
            services.AddSingleton<Services.AI.GeminiAIService>(sp =>
            {
                var toolRegistry = sp.GetRequiredService<Services.AI.AIToolRegistry>();
                var fallbackService = sp.GetRequiredService<Services.AI.DeterministicFallbackAIService>();
                var ragService = sp.GetRequiredService<Services.AI.AutomotiveDiagnosticRAGService>();
                var localOllamaService = sp.GetRequiredService<Services.AI.LocalOllamaAIService>();
                var db = sp.GetService<DatabaseService>();
                string? apiKey = null;
                string model = "gemini-2.0-flash";
                bool enabled = true;
                string customInstructions = string.Empty;

                if (db != null)
                {
                    try
                    {
                        var cfgService = new SystemConfigurationService(db, sp.GetService<LoggerService>());
                        var cfg = cfgService.LoadOrCreate(App.RuntimeAppDataPath);
                        apiKey = cfg.GeminiApiKey;
                        if (!string.IsNullOrWhiteSpace(cfg.GeminiModel)) model = cfg.GeminiModel;
                        enabled = cfg.GeminiEnabled;
                        customInstructions = cfg.GeminiCustomInstructions;
                    }
                    catch { }
                }

                var service = new Services.AI.GeminiAIService(toolRegistry, fallbackService, ragService, localOllamaService, apiKey, model);
                service.DefinirHabilitado(enabled);
                service.DefinirInstrucoesPersonalizadas(customInstructions);
                return service;
            });
            services.AddSingleton<Services.AI.IAIService>(sp => sp.GetRequiredService<Services.AI.GeminiAIService>());
            services.AddTransient<UserControls.AiDiagnosticCenterControl>();

            return services;
        }

        public static IServiceCollection AddPrimoAutoEletricaViewModels(this IServiceCollection services)
        {
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ClientesViewModel>();
            services.AddTransient<OrcamentosViewModel>();
            services.AddTransient<AgendamentosViewModel>();
            services.AddTransient<FinanceiroViewModel>();
            services.AddTransient<RelatoriosViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<PDVViewModel>();
            services.AddTransient<EstoqueViewModel>();
            services.AddTransient<TrocarSenhaObrigatoriaViewModel>();
            services.AddTransient<NovoAgendamentoPremiumViewModel>();
            services.AddTransient<ClienteListItemViewModel>();
            services.AddTransient<FornecedoresViewModel>();
            services.AddTransient<OrdensServicoViewModel>();
            services.AddTransient<VeiculosViewModel>();
            services.AddTransient<AutoEletricaTecnicaViewModel>();
            services.AddTransient<CatalogoPecasViewModel>();
            services.AddTransient<PrinterManagementViewModel>();
            services.AddTransient<RelatoriosModernoViewModel>();

            services.AddTransient<FuncionariosViewModel>(sp =>
            {
                var repo = sp.GetRequiredService<IFuncionarioRepository>();
                var logado = App.Session?.CurrentUser as Funcionario ?? new Funcionario { Nome = "Sistema", PerfilAcesso = "Administrador", Ativo = true };
                return new FuncionariosViewModel(repo, logado);
            });

            return services;
        }

        public static IServiceCollection AddPrimoAutoEletricaRepositories(this IServiceCollection services)
        {
            services.AddSingleton<IFuncionarioRepository>(sp => sp.GetRequiredService<RepositoryRegistry>().Funcionarios);
            services.AddSingleton<IClienteRepository>(sp => sp.GetRequiredService<RepositoryRegistry>().Clientes);
            services.AddSingleton<IProdutoRepository>(sp => sp.GetRequiredService<RepositoryRegistry>().Produtos);
            services.AddSingleton<IFornecedorRepository>(sp => sp.GetRequiredService<RepositoryRegistry>().Fornecedores);
            services.AddSingleton<IOrdemServicoRepository>(sp => sp.GetRequiredService<RepositoryRegistry>().OrdensServico);
            return services;
        }

        public static IServiceCollection AddPrimoAutoEletricaLogging(this IServiceCollection services)
        {
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Information);
            });
            return services;
        }
    }
}
