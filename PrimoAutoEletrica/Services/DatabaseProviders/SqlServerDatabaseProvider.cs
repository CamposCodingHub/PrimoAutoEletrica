using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PrimoAutoEletrica.Services.DatabaseProviders
{
    /// <summary>
    /// Provider de banco de dados SQL Server.
    /// </summary>
    public class SqlServerDatabaseProvider : IDatabaseProvider
    {
        private readonly string _serverName;
        private readonly string _instanceName;
        private readonly string _databaseName;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _useWindowsAuthentication;
        private readonly int _connectionTimeout;
        private readonly LoggerService _logger;
        private readonly string _connectionString;

        public SqlServerDatabaseProvider(
            string serverName,
            string instanceName,
            string databaseName,
            bool useWindowsAuthentication,
            string? username = null,
            string? password = null,
            int connectionTimeout = 30,
            LoggerService? logger = null)
        {
            _serverName = serverName ?? throw new ArgumentNullException(nameof(serverName));
            _instanceName = instanceName ?? string.Empty;
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            _useWindowsAuthentication = useWindowsAuthentication;
            _username = username ?? string.Empty;
            _password = password ?? string.Empty;
            _connectionTimeout = connectionTimeout;
            _logger = logger ?? new LoggerService();

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = string.IsNullOrWhiteSpace(_instanceName) ? _serverName : $"{_serverName}\\{_instanceName}",
                InitialCatalog = _databaseName,
                ConnectTimeout = _connectionTimeout,
                IntegratedSecurity = _useWindowsAuthentication,
                TrustServerCertificate = true
            };

            if (!_useWindowsAuthentication)
            {
                if (string.IsNullOrWhiteSpace(_username))
                    throw new ArgumentException("Username is required when not using Windows authentication.", nameof(username));
                if (string.IsNullOrWhiteSpace(_password))
                    throw new ArgumentException("Password is required when not using Windows authentication.", nameof(password));

                builder.UserID = _username;
                builder.Password = _password;
            }

            _connectionString = builder.ConnectionString;
        }

        public string ProviderType => "SQL Server";
        public string ConnectionString => _connectionString;
        public string DatabaseName => _databaseName;
        public string ServerName => string.IsNullOrWhiteSpace(_instanceName) ? _serverName : $"{_serverName}\\{_instanceName}";

        public DbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
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
                versionCommand.CommandText = "SELECT @@VERSION;";
                var version = versionCommand.ExecuteScalar()?.ToString() ?? "Unknown";

                _logger.LogInfo($"Teste de conexao SQL Server bem-sucedido em {latency.TotalMilliseconds:F0}ms. Servidor: {ServerName}, Banco: {DatabaseName}. Versao: {version}");

                return DatabaseConnectionTestResult.Successful(
                    $"Conexao com SQL Server realizada com sucesso em {ServerName}.",
                    version,
                    latency);
            }
            catch (SqlException ex)
            {
                var latency = DateTime.Now - startTime;
                var errorMessage = InterpretSqlError(ex);
                _logger.LogError($"Falha no teste de conexao SQL Server em {latency.TotalMilliseconds:F0}ms. Servidor: {ServerName}, Banco: {DatabaseName}. Erro: {errorMessage}", ex);

                return DatabaseConnectionTestResult.Failed(errorMessage);
            }
            catch (Exception ex)
            {
                var latency = DateTime.Now - startTime;
                _logger.LogError($"Falha inesperada no teste de conexao SQL Server em {latency.TotalMilliseconds:F0}ms. Servidor: {ServerName}, Banco: {DatabaseName}.", ex);

                return DatabaseConnectionTestResult.Failed(ex.Message);
            }
        }

        private string InterpretSqlError(SqlException ex)
        {
            return ex.Number switch
            {
                2 or 53 => "Servidor SQL Server nao encontrado ou inacessivel. Verifique o nome do servidor, instância e conectividade de rede.",
                18456 => "Autenticacao falhou. Verifique o usuario e senha.",
                4060 or 4064 => $"Banco de dados '{_databaseName}' nao encontrado ou nao pode ser acessado.",
                18452 => "Login falhou. O usuario nao tem permissao para acessar este servidor.",
                18487 or 18488 => "Usuario ou senha invalidos.",
                547 => "Violacao de constraint. A operacao violou uma regra de integridade.",
                2601 or 2627 => "Violacao de chave primaria ou unique constraint.",
                _ => $"Erro SQL Server ({ex.Number}): {ex.Message}"
            };
        }

        public bool CreateDatabaseIfNotExists()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString)
                {
                    InitialCatalog = "master"
                };

                using var connection = new SqlConnection(builder.ConnectionString);
                connection.Open();

                var checkCommand = connection.CreateCommand();
                checkCommand.CommandText = @"
                    SELECT COUNT(*) FROM sys.databases 
                    WHERE name = @DatabaseName;";
                var param = checkCommand.CreateParameter();
                param.ParameterName = "@DatabaseName";
                param.Value = _databaseName;
                checkCommand.Parameters.Add(param);

                var exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;

                if (!exists)
                {
                    _logger.LogInfo($"Criando banco SQL Server '{_databaseName}' no servidor '{ServerName}'.");

                    var createCommand = connection.CreateCommand();
                    createCommand.CommandText = $@"
                        CREATE DATABASE [{_databaseName}]
                        COLLATE Latin1_General_CI_AS;";
                    createCommand.ExecuteNonQuery();

                    _logger.LogInfo($"Banco SQL Server '{_databaseName}' criado com sucesso.");
                }

                return true;
            }
            catch (SqlException ex)
            {
                var errorMessage = InterpretSqlError(ex);
                _logger.LogError($"Falha ao criar banco SQL Server '{_databaseName}' no servidor '{ServerName}'. Erro: {errorMessage}", ex);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha inesperada ao criar banco SQL Server '{_databaseName}' no servidor '{ServerName}'.", ex);
                return false;
            }
        }

        public bool CreateSchema()
        {
            try
            {
                _logger.LogInfo("Iniciando criacao/atualizacao de schema SQL Server usando script SqlServerSchema.sql.");

                // Localizar o arquivo de script relativo ao diretório de runtime
                var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
                string[] candidates = new[]
                {
                    Path.Combine(baseDir, "Services", "DatabaseProviders", "SqlServerSchema.sql"),
                    Path.Combine(baseDir, "DatabaseProviders", "SqlServerSchema.sql"),
                    Path.Combine(baseDir, "Services", "SqlServerSchema.sql"),
                };

                string? scriptPath = candidates.FirstOrDefault(File.Exists);

                // Se nao encontrado, subir na arvore de pastas procurando o arquivo
                if (scriptPath == null)
                {
                    var dir = new DirectoryInfo(baseDir);
                    while (dir != null)
                    {
                        var candidate = Path.Combine(dir.FullName, "Services", "DatabaseProviders", "SqlServerSchema.sql");
                        if (File.Exists(candidate))
                        {
                            scriptPath = candidate;
                            break;
                        }

                        dir = dir.Parent;
                    }
                }

                if (scriptPath == null)
                {
                    _logger.LogError("Script SqlServerSchema.sql nao foi encontrado no runtime. Impossivel criar schema automaticamente.");
                    return false;
                }

                var script = File.ReadAllText(scriptPath);

                using var connection = CreateConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = script;
                command.CommandTimeout = _connectionTimeout;
                command.ExecuteNonQuery();

                _logger.LogInfo("Schema SQL Server executado com sucesso a partir do script.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao criar schema SQL Server.", ex);
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
                        SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @Nome;";
                    var param = command.CreateParameter();
                    param.ParameterName = "@Nome";
                    param.Value = tabela;
                    command.Parameters.Add(param);

                    var result = Convert.ToInt32(command.ExecuteScalar());
                    if (result == 0)
                    {
                        tabelasFaltantes.Add(tabela);
                    }
                }

                if (tabelasFaltantes.Count > 0)
                {
                    return SchemaValidationResult.Invalid(
                        $"Schema SQL Server incompleto. Faltam {tabelasFaltantes.Count} tabelas.",
                        tabelasFaltantes.ToArray(),
                        Array.Empty<string>(),
                        Array.Empty<string>());
                }

                return SchemaValidationResult.Valid();
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao validar schema SQL Server.", ex);
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
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString)
                {
                    InitialCatalog = "master"
                };

                using var connection = new SqlConnection(builder.ConnectionString);
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT COUNT(*) FROM sys.databases 
                    WHERE name = @DatabaseName;";
                var param = command.CreateParameter();
                param.ParameterName = "@DatabaseName";
                param.Value = _databaseName;
                command.Parameters.Add(param);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
            catch
            {
                return false;
            }
        }

        public string GetDatabaseVersion()
        {
            try
            {
                using var connection = CreateConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT @@VERSION;";

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
