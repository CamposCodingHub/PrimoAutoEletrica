using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimoAutoEletrica.Services;
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

            services.AddTransient(sp => PermissionService.CriarParaSessaoAtual(
                sp.GetService<LoggerService>(),
                sp.GetService<DatabaseService>()));
            services.AddTransient<RBACService>();
            services.AddTransient<NavigationService>();
            services.AddTransient<OrcamentoDatabaseService>();
            services.AddTransient<EstoqueOperationalService>();
            services.AddTransient<FinanceiroDatabaseService>();

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
