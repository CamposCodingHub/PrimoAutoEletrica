using System;
using System.IO;

namespace PrimoAutoEletrica.Services.DatabaseProviders
{
    /// <summary>
    /// Factory para criação de providers de banco de dados com fallback seguro.
    /// </summary>
    public static class DatabaseProviderFactory
    {
        /// <summary>
        /// Cria um provider de banco de dados baseado nas configurações.
        /// </summary>
        /// <param name="settings">Configurações de conexão.</param>
        /// <param name="logger">Serviço de logger.</param>
        /// <param name="appDataPath">Caminho do diretório de dados da aplicação.</param>
        /// <returns>Provider de banco de dados configurado.</returns>
        public static IDatabaseProvider CreateProvider(
            DatabaseConnectionSettings settings,
            LoggerService logger,
            string appDataPath)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrWhiteSpace(appDataPath))
                throw new ArgumentException("Caminho do diretório de dados não pode ser vazio.", nameof(appDataPath));

            if (settings.IsSQLite)
            {
                var sqlitePath = settings.ResolveSqlitePath(appDataPath);
                logger.LogInfo($"Criando provider SQLite com caminho: '{sqlitePath}'.");
                return new SQLiteDatabaseProvider(sqlitePath, logger);
            }

            if (settings.IsSqlServer)
            {
                logger.LogInfo($"Criando provider SQL Server com servidor: '{settings.SqlServerHost}', banco: '{settings.SqlServerDatabase}'.");
                return new SqlServerDatabaseProvider(
                    settings.SqlServerHost ?? ".\\SQLEXPRESS",
                    settings.SqlServerInstance ?? string.Empty,
                    settings.SqlServerDatabase ?? "PrimoAutoEletrica",
                    settings.UseWindowsAuthentication,
                    settings.SqlServerUsername,
                    settings.SqlServerPassword,
                    settings.CommandTimeoutSeconds,
                    logger);
            }

            logger.LogWarning($"Provider desconhecido: '{settings.Provider}'. Usando SQLite como fallback.");
            var fallbackPath = Path.Combine(appDataPath, settings.SQLitePath ?? "primoauto.db");
            return new SQLiteDatabaseProvider(fallbackPath, logger);
        }

        /// <summary>
        /// Cria um provider de banco de dados com fallback seguro.
        /// Tenta usar o provider configurado, mas fallback para SQLite em caso de falha.
        /// </summary>
        /// <param name="settings">Configurações de conexão.</param>
        /// <param name="logger">Serviço de logger.</param>
        /// <param name="appDataPath">Caminho do diretório de dados da aplicação.</param>
        /// <returns>Provider de banco de dados configurado com fallback.</returns>
        public static IDatabaseProvider CreateProviderWithFallback(
            DatabaseConnectionSettings settings,
            LoggerService logger,
            string appDataPath)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrWhiteSpace(appDataPath))
                throw new ArgumentException("Caminho do diretório de dados não pode ser vazio.", nameof(appDataPath));

            if (settings.IsSQLite)
            {
                var sqlitePath = settings.ResolveSqlitePath(appDataPath);
                logger.LogInfo($"Criando provider SQLite com caminho: '{sqlitePath}'.");
                return new SQLiteDatabaseProvider(sqlitePath, logger);
            }

            if (settings.IsSqlServer)
            {
                try
                {
                    logger.LogInfo($"Tentando criar provider SQL Server com servidor: '{settings.SqlServerHost}', banco: '{settings.SqlServerDatabase}'.");
                    var sqlServerProvider = new SqlServerDatabaseProvider(
                        settings.SqlServerHost ?? ".\\SQLEXPRESS",
                        settings.SqlServerInstance ?? string.Empty,
                        settings.SqlServerDatabase ?? "PrimoAutoEletrica",
                        settings.UseWindowsAuthentication,
                        settings.SqlServerUsername,
                        settings.SqlServerPassword,
                        settings.CommandTimeoutSeconds,
                        logger);

                    var testResult = sqlServerProvider.TestConnection();
                    if (testResult.Success)
                    {
                        logger.LogInfo($"Provider SQL Server criado com sucesso. Conexão testada com latência: {testResult.Latency?.TotalMilliseconds:F0}ms.");
                        return sqlServerProvider;
                    }

                    logger.LogWarning($"Falha ao testar conexão SQL Server: {testResult.ErrorMessage}. Fallback para SQLite.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"Erro ao criar provider SQL Server. Fallback para SQLite.", ex);
                }
            }

            logger.LogWarning($"Fallback para SQLite devido a falha na conexão SQL Server.");
            var fallbackPath = Path.Combine(appDataPath, settings.SQLitePath ?? "primoauto.db");
            return new SQLiteDatabaseProvider(fallbackPath, logger);
        }

        /// <summary>
        /// Cria um provider SQLite para fallback.
        /// </summary>
        /// <param name="logger">Serviço de logger.</param>
        /// <param name="appDataPath">Caminho do diretório de dados da aplicação.</param>
        /// <param name="databasePath">Caminho do banco de dados SQLite (opcional).</param>
        /// <returns>Provider SQLite.</returns>
        public static IDatabaseProvider CreateFallbackProvider(
            LoggerService logger,
            string appDataPath,
            string? databasePath = null)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            if (string.IsNullOrWhiteSpace(appDataPath))
                throw new ArgumentException("Caminho do diretório de dados não pode ser vazio.", nameof(appDataPath));

            var sqlitePath = string.IsNullOrWhiteSpace(databasePath)
                ? Path.Combine(appDataPath, "primoauto.db")
                : Path.IsPathRooted(databasePath)
                    ? databasePath
                    : Path.Combine(appDataPath, databasePath);

            logger.LogInfo($"Criando provider SQLite de fallback com caminho: '{sqlitePath}'.");
            return new SQLiteDatabaseProvider(sqlitePath, logger);
        }
    }
}
