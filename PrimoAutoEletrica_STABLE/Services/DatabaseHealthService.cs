using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Services
{
    public sealed class DatabaseHealthReport
    {
        public bool IntegridadeOk { get; init; }
        public int MigrationsAplicadas { get; init; }
        public List<string> Avisos { get; } = new();
    }

    public sealed class DatabaseHealthService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;

        public DatabaseHealthService(DatabaseService databaseService, LoggerService logger)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public DatabaseHealthReport Verificar()
        {
            var avisos = new List<string>();

            using var connection = _databaseService.GetConnection();
            connection.Open();

            var isSqlServer = string.Equals(_databaseService.RuntimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
            var integridadeOk = VerificarIntegridade(connection, avisos, isSqlServer);
            var migrations = ContarMigrations(connection);
            VerificarTabela(connection, "SchemaMigrations", avisos, isSqlServer);
            VerificarTabela(connection, "SchemaVersion", avisos, isSqlServer);
            VerificarTabela(connection, "AuditLogs", avisos, isSqlServer);
            VerificarTabela(connection, "RegistroBloqueios", avisos, isSqlServer);
            VerificarTabela(connection, "DatabaseBackups", avisos, isSqlServer);

            var report = new DatabaseHealthReport
            {
                IntegridadeOk = integridadeOk,
                MigrationsAplicadas = migrations
            };

            foreach (var aviso in avisos)
            {
                report.Avisos.Add(aviso);
                _logger.LogWarning($"Banco: {aviso}");
            }

            if (integridadeOk)
            {
                _logger.LogInfo($"Saude do banco validada. Migrations aplicadas: {migrations}.");
            }

            return report;
        }

        private static bool VerificarIntegridade(DbConnection connection, List<string> avisos, bool isSqlServer)
        {
            using var command = connection.CreateCommand();
            command.CommandText = isSqlServer ? "SELECT 1;" : "PRAGMA integrity_check;";
            var result = Convert.ToString(command.ExecuteScalar());
            var ok = isSqlServer || string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);

            if (!ok)
            {
                avisos.Add($"Integrity check retornou: {result}");
            }

            return ok;
        }

        private static int ContarMigrations(DbConnection connection)
        {
            var isSqlServer = connection.GetType().FullName?.Contains("SqlClient", StringComparison.OrdinalIgnoreCase) == true;
            if (!TabelaExiste(connection, "SchemaMigrations", isSqlServer))
            {
                return 0;
            }

            using var command = connection.CreateCommand();
            command.CommandText = TabelaExiste(connection, "SchemaVersion", isSqlServer)
                ? "SELECT COUNT(*) FROM SchemaVersion;"
                : "SELECT COUNT(*) FROM SchemaMigrations;";
            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }

        private static void VerificarTabela(DbConnection connection, string tabela, List<string> avisos, bool isSqlServer)
        {
            if (!TabelaExiste(connection, tabela, isSqlServer))
            {
                avisos.Add($"Tabela obrigatoria ausente: {tabela}");
            }
        }

        private static bool TabelaExiste(DbConnection connection, string tabela, bool isSqlServer)
        {
            using var command = connection.CreateCommand();
            command.CommandText = isSqlServer
                ? @"
                SELECT 1
                FROM sys.tables
                WHERE name = @Tabela;"
                : @"
                SELECT 1
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = @Tabela
                LIMIT 1;";
            command.Parameters.AddWithValue("@Tabela", tabela);
            return command.ExecuteScalar() != null;
        }
    }
}
