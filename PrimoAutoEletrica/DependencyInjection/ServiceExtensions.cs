using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Repositories;

namespace PrimoAutoEletrica.DependencyInjection
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddPrimoAutoEletricaCore(this IServiceCollection services)
        {
            // Singleton services - instância única para toda aplicação
            services.AddSingleton<LoggerService>();
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<RepositoryRegistry>();
            services.AddSingleton<AuditLogService>();
            services.AddSingleton<DatabaseBackupService>();
            services.AddSingleton<RegistroBloqueioService>();
            services.AddSingleton<DatabaseHealthService>();
            services.AddSingleton<SynchronizationService>();
            services.AddSingleton<LocalSyncService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<ContabilExportService>();
            services.AddSingleton<MigrationService>();
            services.AddSingleton<AppSessionService>();
            services.AddSingleton<LocalizationService>();
            services.AddSingleton<FilialService>();
            
            // Transient services - nova instância a cada resolução
            services.AddTransient<PermissionService>();
            services.AddTransient<NavigationService>();
            services.AddTransient<OrcamentoDatabaseService>();
            services.AddTransient<EstoqueOperationalService>();
            services.AddTransient<FinanceiroDatabaseService>();
            
            return services;
        }

        public static IServiceCollection AddPrimoAutoEletricaViewModels(this IServiceCollection services)
        {
            // Register all ViewModels (Transient)
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ClientesViewModel>();
            services.AddTransient<OrcamentosViewModel>();
            services.AddTransient<AgendamentosViewModel>();
            services.AddTransient<FinanceiroViewModel>();
            services.AddTransient<RelatoriosViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<PDVViewModel>();
            services.AddTransient<EstoqueViewModel>();
            services.AddTransient<FuncionariosViewModel>();
            services.AddTransient<TrocarSenhaObrigatoriaViewModel>();
            services.AddTransient<NovoAgendamentoPremiumViewModel>();
            services.AddTransient<ClienteListItemViewModel>();
            services.AddTransient<FornecedoresViewModel>();
            services.AddTransient<OrdensServicoViewModel>();
            services.AddTransient<VeiculosViewModel>();
            return services;
        }

        public static IServiceCollection AddPrimoAutoEletricaRepositories(this IServiceCollection services)
        {
            // Register all repositories (Singleton - pois dependem de DatabaseService)
            services.AddSingleton<ClienteRepository>();
            services.AddSingleton<FuncionarioRepository>();
            services.AddSingleton<FornecedorRepository>();
            services.AddSingleton<ProdutoRepository>();
            services.AddSingleton<OrdemServicoRepository>();
            services.AddSingleton<VendaRepository>();
            services.AddSingleton<AuditoriaRepository>();
            
            return services;
        }

        public static IServiceCollection AddPrimoAutoEletricaLogging(this IServiceCollection services)
        {
            // Configure logging (opcional - pode ser expandido)
            services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Information);
            });
            
            return services;
        }
    }
}
