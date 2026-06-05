using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services
{
    public sealed class DatabaseProviderPlan
    {
        public string ProviderAtual { get; init; } = "SQLite";
        public bool SqlServerProntoParaConfigurar { get; init; }
        public List<string> Pendencias { get; } = new();
    }

    public static class DatabaseProviderPlanService
    {
        public static DatabaseProviderPlan Avaliar(DatabaseConnectionSettings settings)
        {
            var plan = new DatabaseProviderPlan
            {
                ProviderAtual = settings.Provider,
                SqlServerProntoParaConfigurar = settings.IsSqlServer &&
                    !string.IsNullOrWhiteSpace(settings.SqlServerHost) &&
                    !string.IsNullOrWhiteSpace(settings.SqlServerDatabase) &&
                    (settings.UseWindowsAuthentication || !string.IsNullOrWhiteSpace(settings.SqlUser))
            };

            if (settings.IsSQLite)
            {
                plan.Pendencias.Add("Provider atual ainda e SQLite. Para migrar, alterar Provider para SqlServer apos criar schema SQL Server.");
            }

            if (settings.IsSqlServer && string.IsNullOrWhiteSpace(settings.SqlServerHost))
            {
                plan.Pendencias.Add("Servidor SQL Server nao configurado.");
            }

            if (settings.IsSqlServer && string.IsNullOrWhiteSpace(settings.SqlServerDatabase))
            {
                plan.Pendencias.Add("Banco SQL Server nao configurado.");
            }

            if (settings.IsSqlServer && !settings.UseWindowsAuthentication && string.IsNullOrWhiteSpace(settings.SqlUser))
            {
                plan.Pendencias.Add("Usuario SQL Server nao configurado.");
            }

            return plan;
        }
    }
}
