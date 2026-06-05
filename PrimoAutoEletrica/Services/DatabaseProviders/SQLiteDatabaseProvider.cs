using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.Data.Common;
using System.IO;

namespace PrimoAutoEletrica.Services.DatabaseProviders
{
    /// <summary>
    /// Provider de banco de dados SQLite.
    /// </summary>
    public class SQLiteDatabaseProvider : IDatabaseProvider
    {
        private readonly string _databasePath;
        private readonly string _connectionString;
        private readonly LoggerService _logger;

        public SQLiteDatabaseProvider(string databasePath, LoggerService logger)
        {
            _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath,
                Cache = SqliteCacheMode.Shared,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = true
            }.ToString();
        }

        public string ProviderType => "SQLite";
        public string ConnectionString => _connectionString;
        public string DatabaseName => Path.GetFileNameWithoutExtension(_databasePath);
        public string ServerName => "Local";

        public DbConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public DatabaseConnectionTestResult TestConnection()
        {
            var startTime = DateTime.Now;

            try
            {
                using var connection = CreateConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                command.ExecuteScalar();

                var latency = DateTime.Now - startTime;

                var versionCommand = connection.CreateCommand();
                versionCommand.CommandText = "SELECT sqlite_version();";
                var version = versionCommand.ExecuteScalar()?.ToString() ?? "Unknown";

                _logger.LogInfo($"Teste de conexao SQLite bem-sucedido em {latency.TotalMilliseconds:F0}ms. Versao: {version}");

                return DatabaseConnectionTestResult.Successful(
                    "Conexao com SQLite realizada com sucesso.",
                    version,
                    latency);
            }
            catch (Exception ex)
            {
                var latency = DateTime.Now - startTime;
                _logger.LogError($"Falha no teste de conexao SQLite em {latency.TotalMilliseconds:F0}ms.", ex);

                return DatabaseConnectionTestResult.Failed(ex.Message);
            }
        }

        public bool CreateDatabaseIfNotExists()
        {
            try
            {
                var directory = Path.GetDirectoryName(_databasePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(_databasePath))
                {
                    _logger.LogInfo($"Criando banco SQLite em '{_databasePath}'.");
                    using var connection = CreateConnection();
                    connection.Open();
                    connection.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao criar banco SQLite em '{_databasePath}'.", ex);
                return false;
            }
        }

        public bool CreateSchema()
        {
            try
            {
                _logger.LogInfo("Criacao de schema SQLite ainda delegada ao DatabaseService existente.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao criar schema SQLite.", ex);
                return false;
            }
        }

        public SchemaValidationResult ValidateSchema()
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();

                var tabelasEsperadas = new[]
                {
                    "Funcionarios",
                    "Clientes",
                    "Produtos",
                    "Fornecedores",
                    "Vendas",
                    "VendaItens",
                    "Veiculos",
                    "OrdensServico",
                    "OrdemServicoItens",
                    "OrdemServicoEventos",
                    "AuditLogs",
                    "Permissoes",
                    "PerfisAcesso",
                    "PerfilPermissoes",
                    "LoginTentativasSeguranca"
                };

                var tabelasFaltantes = new List<string>();

                foreach (var tabela in tabelasEsperadas)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT name FROM sqlite_master 
                        WHERE type='table' AND name=@Nome;";
                    var param = command.CreateParameter();
                    param.ParameterName = "@Nome";
                    param.Value = tabela;
                    command.Parameters.Add(param);

                    var result = command.ExecuteScalar();
                    if (result == null)
                    {
                        tabelasFaltantes.Add(tabela);
                    }
                }

                if (tabelasFaltantes.Count > 0)
                {
                    return SchemaValidationResult.Invalid(
                        $"Schema SQLite incompleto. Faltam {tabelasFaltantes.Count} tabelas.",
                        tabelasFaltantes.ToArray(),
                        Array.Empty<string>(),
                        Array.Empty<string>());
                }

                return SchemaValidationResult.Valid();
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao validar schema SQLite.", ex);
                return SchemaValidationResult.Invalid(
                    $"Erro ao validar schema: {ex.Message}",
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    Array.Empty<string>());
            }
        }

        public DbTransaction BeginTransaction(DbConnection connection)
        {
            return connection.BeginTransaction();
        }

        public int ExecuteNonQuery(string sql, params (string name, object value)[] parameters)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            foreach (var (name, value) in parameters)
            {
                var param = command.CreateParameter();
                param.ParameterName = name;
                param.Value = value ?? DBNull.Value;
                command.Parameters.Add(param);
            }

            return command.ExecuteNonQuery();
        }

        public object ExecuteScalar(string sql, params (string name, object value)[] parameters)
        {
            using var connection = CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;

            foreach (var (name, value) in parameters)
            {
                var param = command.CreateParameter();
                param.ParameterName = name;
                param.Value = value ?? DBNull.Value;
                command.Parameters.Add(param);
            }

            return command.ExecuteScalar() ?? DBNull.Value;
        }

        public IDataReader ExecuteReader(string sql, params (string name, object value)[] parameters)
        {
            var connection = CreateConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = sql;

            foreach (var (name, value) in parameters)
            {
                var param = command.CreateParameter();
                param.ParameterName = name;
                param.Value = value ?? DBNull.Value;
                command.Parameters.Add(param);
            }

            return command.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public bool DatabaseExists()
        {
            return File.Exists(_databasePath);
        }

        public string GetDatabaseVersion()
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT sqlite_version();";

                var version = command.ExecuteScalar()?.ToString();
                return version ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public void Dispose()
        {
        }
    }
}
