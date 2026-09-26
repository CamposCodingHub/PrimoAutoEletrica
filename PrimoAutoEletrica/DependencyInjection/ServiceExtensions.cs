using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Services.Fiscal;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services.Knowledge;
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
            services.AddSingleton<IDiagnosticoTecnicoService, DiagnosticoTecnicoService>();
            services.AddSingleton<AppCacheService>();
            services.AddSingleton<SoftDeleteService>();

            // FundaÃ§Ã£o fiscal â€” Focus adapter; produÃ§Ã£o bloqueada; Fake apenas em testes.
            services.AddSingleton(sp => new FiscalConfigurationService(
                App.RuntimeAppDataPath,
                sp.GetService<LoggerService>()));
            services.AddSingleton<FocusNfeHttpClient>();
            services.AddSingleton<FiscalOperationStore>();
            services.AddSingleton<FiscalEmpresaStore>();
            services.AddSingleton(sp => new FiscalArtifactStorage(App.RuntimeAppDataPath));
            services.AddSingleton<IDanfeGenerator, DanfeInformationalPdfGenerator>();
            services.AddSingleton<ICertificateProvider, NullCertificateProvider>();
            services.AddSingleton<IXmlSigner, BlockedXmlSigner>();
            services.AddSingleton<IWhatsAppProvider, ManualWhatsAppProvider>();
            services.AddSingleton<IFiscalWebhookProcessor, FiscalWebhookProcessor>();
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

            // C2.1/C2.2 Knowledge retrieval + deterministic search (no scattered new KnowledgeSearchService())
            services.AddSingleton<IKnowledgeRepository>(sp => sp.GetRequiredService<RepositoryRegistry>().Knowledge);
            services.AddSingleton<IKnowledgeRetrievalService>(sp =>
                new KnowledgeRetrievalService(
                    sp.GetRequiredService<IKnowledgeRepository>(),
                    sp.GetService<IOrdemServicoRepository>(),
                    sp.GetService<LoggerService>()));
            services.AddSingleton<IKnowledgeSearchService>(sp =>
                new KnowledgeSearchService(
                    sp.GetRequiredService<IKnowledgeRetrievalService>(),
                    sp.GetService<LoggerService>()));

            // C2.3 Context Engine — proven relations only (no invented joins)
            services.AddSingleton<IVehicleContextService>(sp =>
                new VehicleContextService(
                    sp.GetRequiredService<RepositoryRegistry>().Clientes,
                    sp.GetRequiredService<RepositoryRegistry>().OrdensServico,
                    sp.GetRequiredService<IKnowledgeRepository>(),
                    sp.GetService<LoggerService>()));
            services.AddSingleton<IClientContextService>(sp =>
                new ClientContextService(
                    sp.GetRequiredService<RepositoryRegistry>().Clientes,
                    sp.GetRequiredService<RepositoryRegistry>().OrdensServico,
                    sp.GetService<LoggerService>()));
            services.AddSingleton<IWorkOrderContextService>(sp =>
                new WorkOrderContextService(
                    sp.GetRequiredService<RepositoryRegistry>().OrdensServico,
                    sp.GetRequiredService<RepositoryRegistry>().Clientes,
                    sp.GetRequiredService<IKnowledgeRepository>(),
                    sp.GetService<LoggerService>()));
            services.AddSingleton<IContextCompositionService>(sp =>
                new ContextCompositionService(
                    sp.GetRequiredService<IVehicleContextService>(),
                    sp.GetRequiredService<IClientContextService>(),
                    sp.GetRequiredService<IWorkOrderContextService>(),
                    sp.GetService<LoggerService>()));

            
            services.AddSingleton<IIntelligence360Enricher>(sp =>
                new Intelligence360Enricher(sp.GetRequiredService<IContextCompositionService>()));
            services.AddSingleton<IDiagnosticIntelligenceService>(sp =>
                new DiagnosticIntelligenceService(
                    sp.GetRequiredService<IContextualSearchService>(),
                    sp.GetRequiredService<IContextCompositionService>(),
                    sp.GetRequiredService<IKnowledgeRetrievalService>()));
            services.AddSingleton<IKnowledgePromotionService>(sp =>
                new KnowledgePromotionService(
                    sp.GetRequiredService<RepositoryRegistry>().OrdensServico,
                    sp.GetRequiredService<IKnowledgeRepository>(),
                    sp.GetRequiredService<IWorkOrderContextService>()));
            services.AddSingleton<IIntelligenceAuditService, IntelligenceAuditService>();
            services.AddSingleton<IContextualSearchService>(sp =>
                new ContextualSearchService(
                    sp.GetRequiredService<IKnowledgeSearchService>(),
                    sp.GetRequiredService<IContextCompositionService>(),
                    sp.GetService<LoggerService>()));
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
            services.AddTransient<KnowledgeSearchViewModel>(sp =>
            {
                var search = sp.GetRequiredService<IKnowledgeSearchService>();
                return new KnowledgeSearchViewModel(search, () =>
                {
                    try
                    {
                        var perms = PermissionService.CriarParaSessaoAtual(
                            sp.GetService<LoggerService>(),
                            sp.GetService<DatabaseService>());
                        return perms.TemPermissao("Financeiro") || perms.TemPermissaoCodigo("FINANCEIRO_VER");
                    }
                    catch
                    {
                        return false;
                    }
                });
            });

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
            services.AddSingleton<IPrimox360Service>(sp =>
                new Primox360Service(
                    sp.GetRequiredService<IClienteRepository>(),
                    sp.GetRequiredService<IOrdemServicoRepository>(),
                    produtos: sp.GetRequiredService<IProdutoRepository>()));
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


