using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Services.DatabaseProviders
{
    /// <summary>
    /// Interface para provider de banco de dados, abstraindo SQLite e SQL Server.
    /// </summary>
    public interface IDatabaseProvider : IDisposable
    {
        /// <summary>
        /// Tipo do provider (SQLite ou SQL Server).
        /// </summary>
        string ProviderType { get; }

        /// <summary>
        /// Connection string do banco de dados.
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Nome do banco de dados.
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        /// Servidor/instância do banco de dados.
        /// </summary>
        string ServerName { get; }

        /// <summary>
        /// Cria uma nova conexão com o banco de dados.
        /// </summary>
        DbConnection CreateConnection();

        /// <summary>
        /// Testa a conexão com o banco de dados.
        /// </summary>
        /// <returns>Resultado do teste de conexão.</returns>
        DatabaseConnectionTestResult TestConnection();

        /// <summary>
        /// Cria o banco de dados se não existir.
        /// </summary>
        /// <returns>True se o banco foi criado ou já existia, false se falhou.</returns>
        bool CreateDatabaseIfNotExists();

        /// <summary>
        /// Cria o schema do banco de dados (tabelas, índices, etc.).
        /// </summary>
        /// <returns>True se o schema foi criado com sucesso, false se falhou.</returns>
        bool CreateSchema();

        /// <summary>
        /// Valida se o schema do banco de dados está completo.
        /// </summary>
        /// <returns>Resultado da validação do schema.</returns>
        SchemaValidationResult ValidateSchema();

        /// <summary>
        /// Inicia uma transação.
        /// </summary>
        DbTransaction BeginTransaction(DbConnection connection);

        /// <summary>
        /// Executa um comando SQL que não retorna dados.
        /// </summary>
        /// <param name="sql">Comando SQL a executar.</param>
        /// <param name="parameters">Parâmetros do comando.</param>
        /// <returns>Número de linhas afetadas.</returns>
        int ExecuteNonQuery(string sql, params (string name, object value)[] parameters);

        /// <summary>
        /// Executa um comando SQL e retorna um escalar.
        /// </summary>
        /// <param name="sql">Comando SQL a executar.</param>
        /// <param name="parameters">Parâmetros do comando.</param>
        /// <returns>Valor escalar retornado.</returns>
        object ExecuteScalar(string sql, params (string name, object value)[] parameters);

        /// <summary>
        /// Executa um comando SQL e retorna um reader.
        /// </summary>
        /// <param name="sql">Comando SQL a executar.</param>
        /// <param name="parameters">Parâmetros do comando.</param>
        /// <returns>DataReader com os resultados.</returns>
        IDataReader ExecuteReader(string sql, params (string name, object value)[] parameters);

        /// <summary>
        /// Verifica se o banco de dados existe.
        /// </summary>
        /// <returns>True se o banco existe, false caso contrário.</returns>
        bool DatabaseExists();

        /// <summary>
        /// Obtém a versão do banco de dados.
        /// </summary>
        /// <returns>Versão do banco de dados.</returns>
        string GetDatabaseVersion();
    }

    /// <summary>
    /// Resultado do teste de conexão com o banco de dados.
    /// </summary>
    public class DatabaseConnectionTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public TimeSpan? Latency { get; set; }
        public string? ServerVersion { get; set; }
        public DateTime TestTime { get; set; } = DateTime.Now;

        public static DatabaseConnectionTestResult Successful(string message, string serverVersion, TimeSpan latency)
        {
            return new DatabaseConnectionTestResult
            {
                Success = true,
                Message = message,
                ServerVersion = serverVersion,
                Latency = latency
            };
        }

        public static DatabaseConnectionTestResult Failed(string errorMessage)
        {
            return new DatabaseConnectionTestResult
            {
                Success = false,
                Message = "Falha na conexão com o banco de dados.",
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Resultado da validação do schema do banco de dados.
    /// </summary>
    public class SchemaValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string[] MissingTables { get; set; } = Array.Empty<string>();
        public string[] MissingColumns { get; set; } = Array.Empty<string>();
        public string[] MissingIndexes { get; set; } = Array.Empty<string>();
        public DateTime ValidationTime { get; set; } = DateTime.Now;

        public static SchemaValidationResult Valid()
        {
            return new SchemaValidationResult
            {
                IsValid = true,
                Message = "Schema do banco de dados está completo e válido."
            };
        }

        public static SchemaValidationResult Invalid(string message, string[] missingTables, string[] missingColumns, string[] missingIndexes)
        {
            return new SchemaValidationResult
            {
                IsValid = false,
                Message = message,
                MissingTables = missingTables,
                MissingColumns = missingColumns,
                MissingIndexes = missingIndexes
            };
        }
    }
}
