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

            var integridadeOk = VerificarIntegridade(connection, avisos);
            var migrations = ContarMigrations(connection);
            VerificarTabela(connection, "SchemaMigrations", avisos);
            VerificarTabela(connection, "AuditLogs", avisos);
            VerificarTabela(connection, "RegistroBloqueios", avisos);
            VerificarTabela(connection, "DatabaseBackups", avisos);

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

        private static bool VerificarIntegridade(SqliteConnection connection, List<string> avisos)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = Convert.ToString(command.ExecuteScalar());
            var ok = string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);

            if (!ok)
            {
                avisos.Add($"Integrity check retornou: {result}");
            }

            return ok;
        }

        private static int ContarMigrations(SqliteConnection connection)
        {
            if (!TabelaExiste(connection, "SchemaMigrations"))
            {
                return 0;
            }

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations;";
            return Convert.ToInt32(command.ExecuteScalar() ?? 0);
        }

        private static void VerificarTabela(SqliteConnection connection, string tabela, List<string> avisos)
        {
            if (!TabelaExiste(connection, tabela))
            {
                avisos.Add($"Tabela obrigatoria ausente: {tabela}");
            }
        }

        private static bool TabelaExiste(SqliteConnection connection, string tabela)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
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
