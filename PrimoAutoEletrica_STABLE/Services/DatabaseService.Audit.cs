using Microsoft.Data.Sqlite;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        private void InitializeAuditSchema(DbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id TEXT PRIMARY KEY,
                    DataHora TEXT NOT NULL,
                    Categoria TEXT NOT NULL,
                    Acao TEXT NOT NULL,
                    Entidade TEXT,
                    EntidadeId TEXT,
                    Detalhes TEXT,
                    ValorAnterior TEXT,
                    ValorNovo TEXT,
                    Severidade TEXT NOT NULL,
                    Sucesso INTEGER NOT NULL DEFAULT 1,
                    UsuarioId INTEGER,
                    UsuarioNome TEXT,
                    Perfil TEXT,
                    SessaoId TEXT,
                    Maquina TEXT,
                    CorrelationId TEXT
                );";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_AuditLogs_DataHora
                ON AuditLogs (DataHora DESC);";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_AuditLogs_Categoria_Acao
                ON AuditLogs (Categoria, Acao);";
            command.ExecuteNonQuery();

            command.CommandText = @"
                CREATE INDEX IF NOT EXISTS IX_AuditLogs_Entidade
                ON AuditLogs (Entidade, EntidadeId);";
            command.ExecuteNonQuery();
        }
    }
}
