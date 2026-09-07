using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Serviço especializado para gerenciar migrações de banco de dados de forma estruturada
    /// </summary>
    public class MigrationService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private readonly string _migrationScriptDirectory;

        public MigrationService(DatabaseService databaseService, LoggerService logger, string? migrationScriptDirectory = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _migrationScriptDirectory = migrationScriptDirectory ?? 
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations");
        }

        /// <summary>
        /// Executa todas as migrações pendentes
        /// </summary>
        public void ExecutePendingMigrations()
        {
            try
            {
                _logger.LogInfo("[MigrationService] Iniciando verificação de migrações pendentes");

                using var connection = _databaseService.GetConnection();
                connection.Open();

                InitializeMigrationSchema(connection);

                var pendingMigrations = GetPendingMigrations(connection);
                
                if (pendingMigrations.Count == 0)
                {
                    _logger.LogInfo("[MigrationService] Nenhuma migração pendente encontrada");
                    return;
                }

                _logger.LogInfo($"[MigrationService] {pendingMigrations.Count} migrações pendentes encontradas");

                foreach (var migration in pendingMigrations)
                {
                    ExecuteMigration(connection, migration);
                }

                _logger.LogInfo("[MigrationService] Todas as migrações pendentes foram aplicadas com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError("[MigrationService] Erro ao executar migrações pendentes", ex);
                throw;
            }
        }

        /// <summary>
        /// Cria uma nova migração e registra no sistema
        /// </summary>
        public void CreateMigration(string id, string description, string sqlScript)
        {
            try
            {
                _logger.LogInfo($"[MigrationService] Criando nova migração: {id} - {description}");

                using var connection = _databaseService.GetConnection();
                connection.Open();

                InitializeMigrationSchema(connection);

                // Verificar se a migração já existe
                if (MigrationExists(connection, id))
                {
                    _logger.LogWarning($"[MigrationService] Migração {id} já existe");
                    return;
                }

                // Salvar o script SQL em arquivo
                SaveMigrationScript(id, sqlScript);

                // Registrar a migração no banco
                RegisterMigration(connection, id, description);

                _logger.LogInfo($"[MigrationService] Migração {id} criada com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MigrationService] Erro ao criar migração {id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Obtém o status atual das migrações
        /// </summary>
        public MigrationStatus GetMigrationStatus()
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                InitializeMigrationSchema(connection);

                var appliedMigrations = GetAppliedMigrations(connection);
                var totalMigrations = GetTotalMigrationCount();

                return new MigrationStatus
                {
                    TotalMigrations = totalMigrations,
                    AppliedMigrations = appliedMigrations.Count,
                    PendingMigrations = totalMigrations - appliedMigrations.Count,
                    LastAppliedMigration = appliedMigrations.Count > 0 ? appliedMigrations[^1] : null,
                    IsUpToDate = appliedMigrations.Count >= totalMigrations
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("[MigrationService] Erro ao obter status das migrações", ex);
                return new MigrationStatus { IsUpToDate = false };
            }
        }

        /// <summary>
        /// Reverte a última migração aplicada
        /// </summary>
        public void RollbackLastMigration()
        {
            try
            {
                _logger.LogInfo("[MigrationService] Iniciando rollback da última migração");

                using var connection = _databaseService.GetConnection();
                connection.Open();

                var appliedMigrations = GetAppliedMigrations(connection);
                
                if (appliedMigrations.Count == 0)
                {
                    _logger.LogWarning("[MigrationService] Nenhuma migração aplicada para reverter");
                    return;
                }

                var lastMigration = appliedMigrations[^1];
                
                // Executar script de rollback se existir
                var rollbackScript = GetRollbackScript(lastMigration);
                if (!string.IsNullOrEmpty(rollbackScript))
                {
                    ExecuteSqlScript(connection, rollbackScript);
                }

                // Remover registro da migração
                RemoveMigrationRecord(connection, lastMigration);

                _logger.LogInfo($"[MigrationService] Migração {lastMigration} revertida com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError("[MigrationService] Erro ao reverter última migração", ex);
                throw;
            }
        }

        private void InitializeMigrationSchema(IDbConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS SchemaMigrations
                (
                    Id TEXT PRIMARY KEY,
                    Descricao TEXT NOT NULL,
                    AplicadaEm TEXT NOT NULL,
                    VersaoAplicacao TEXT,
                    Checksum TEXT
                )";
            command.ExecuteNonQuery();
        }

        private List<string> GetPendingMigrations(IDbConnection connection)
        {
            var appliedMigrations = GetAppliedMigrations(connection);
            var allMigrations = GetAvailableMigrations();
            
            var pending = new List<string>();
            foreach (var migration in allMigrations)
            {
                if (!appliedMigrations.Contains(migration))
                {
                    pending.Add(migration);
                }
            }
            
            return pending;
        }

        private List<string> GetAppliedMigrations(IDbConnection connection)
        {
            var applied = new List<string>();
            
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id FROM SchemaMigrations ORDER BY AplicadaEm";
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                applied.Add(reader.GetString(0));
            }
            
            return applied;
        }

        private List<string> GetAvailableMigrations()
        {
            var migrations = new List<string>();
            
            if (!Directory.Exists(_migrationScriptDirectory))
            {
                return migrations;
            }

            var files = Directory.GetFiles(_migrationScriptDirectory, "*.sql");
            foreach (var file in files)
            {
                var migrationId = Path.GetFileNameWithoutExtension(file);
                migrations.Add(migrationId);
            }
            
            migrations.Sort();
            return migrations;
        }

        private int GetTotalMigrationCount()
        {
            if (!Directory.Exists(_migrationScriptDirectory))
            {
                return 0;
            }

            return Directory.GetFiles(_migrationScriptDirectory, "*.sql").Length;
        }

        private bool MigrationExists(IDbConnection connection, string migrationId)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM SchemaMigrations WHERE Id = @Id";
            
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@Id";
            parameter.Value = migrationId;
            command.Parameters.Add(parameter);
            
            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        private void ExecuteMigration(IDbConnection connection, string migrationId)
        {
            try
            {
                _logger.LogInfo($"[MigrationService] Executando migração: {migrationId}");

                var script = LoadMigrationScript(migrationId);
                if (string.IsNullOrEmpty(script))
                {
                    _logger.LogWarning($"[MigrationService] Script não encontrado para migração {migrationId}");
                    return;
                }

                ExecuteSqlScript(connection, script);
                RegisterMigration(connection, migrationId, "Migração aplicada automaticamente");

                _logger.LogInfo($"[MigrationService] Migração {migrationId} aplicada com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MigrationService] Erro ao executar migração {migrationId}", ex);
                throw;
            }
        }

        private void ExecuteSqlScript(IDbConnection connection, string script)
        {
            var statements = script.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var statement in statements)
            {
                var trimmedStatement = statement.Trim();
                if (string.IsNullOrEmpty(trimmedStatement))
                {
                    continue;
                }

                using var command = connection.CreateCommand();
                command.CommandText = trimmedStatement;
                command.ExecuteNonQuery();
            }
        }

        private void RegisterMigration(IDbConnection connection, string id, string description)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO SchemaMigrations (Id, Descricao, AplicadaEm, VersaoAplicacao, Checksum)
                VALUES (@Id, @Descricao, @AplicadaEm, @VersaoAplicacao, @Checksum)";
            
            var idParam = command.CreateParameter();
            idParam.ParameterName = "@Id";
            idParam.Value = id;
            command.Parameters.Add(idParam);
            
            var descParam = command.CreateParameter();
            descParam.ParameterName = "@Descricao";
            descParam.Value = description;
            command.Parameters.Add(descParam);
            
            var dataParam = command.CreateParameter();
            dataParam.ParameterName = "@AplicadaEm";
            dataParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            command.Parameters.Add(dataParam);
            
            var versaoParam = command.CreateParameter();
            versaoParam.ParameterName = "@VersaoAplicacao";
            versaoParam.Value = GetApplicationVersion();
            command.Parameters.Add(versaoParam);
            
            var checksumParam = command.CreateParameter();
            checksumParam.ParameterName = "@Checksum";
            checksumParam.Value = ComputeChecksum(id);
            command.Parameters.Add(checksumParam);
            
            command.ExecuteNonQuery();
        }

        private void RemoveMigrationRecord(IDbConnection connection, string migrationId)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM SchemaMigrations WHERE Id = @Id";
            
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@Id";
            parameter.Value = migrationId;
            command.Parameters.Add(parameter);
            
            command.ExecuteNonQuery();
        }

        private string LoadMigrationScript(string migrationId)
        {
            var scriptPath = Path.Combine(_migrationScriptDirectory, $"{migrationId}.sql");
            
            if (!File.Exists(scriptPath))
            {
                return string.Empty;
            }

            return File.ReadAllText(scriptPath, Encoding.UTF8);
        }

        private string GetRollbackScript(string migrationId)
        {
            var rollbackPath = Path.Combine(_migrationScriptDirectory, $"{migrationId}.rollback.sql");
            
            if (!File.Exists(rollbackPath))
            {
                return string.Empty;
            }

            return File.ReadAllText(rollbackPath, Encoding.UTF8);
        }

        private void SaveMigrationScript(string migrationId, string script)
        {
            if (!Directory.Exists(_migrationScriptDirectory))
            {
                Directory.CreateDirectory(_migrationScriptDirectory);
            }

            var scriptPath = Path.Combine(_migrationScriptDirectory, $"{migrationId}.sql");
            File.WriteAllText(scriptPath, script, Encoding.UTF8);
        }

        private string ComputeChecksum(string data)
        {
            using var hash = System.Security.Cryptography.SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(data);
            var hashBytes = hash.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes);
        }

        private string GetApplicationVersion()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly()
                    .GetName().Version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    /// <summary>
    /// Status atual das migrações do banco de dados
    /// </summary>
    public class MigrationStatus
    {
        public int TotalMigrations { get; set; }
        public int AppliedMigrations { get; set; }
        public int PendingMigrations { get; set; }
        public string? LastAppliedMigration { get; set; }
        public bool IsUpToDate { get; set; }
    }
}