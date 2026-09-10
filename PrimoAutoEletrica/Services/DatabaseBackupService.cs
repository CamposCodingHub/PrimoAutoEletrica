using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using PrimoAutoEletrica.Helpers;
namespace PrimoAutoEletrica.Services
{
    public class DatabaseBackupService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private readonly string _backupDirectory;
        private readonly string? _networkBackupDirectory;
        private Timer? _backupAutomaticoTimer;
        private readonly TimeSpan _intervaloBackupAutomatico = TimeSpan.FromHours(24); // 24 horas

        public DatabaseBackupService(DatabaseService databaseService, LoggerService logger, string? backupDirectory = null, string? networkBackupDirectory = null)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backupDirectory = string.IsNullOrWhiteSpace(backupDirectory)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica", "Backups")
                : backupDirectory;
            _networkBackupDirectory = networkBackupDirectory;

            Directory.CreateDirectory(_backupDirectory);
            if (!string.IsNullOrWhiteSpace(_networkBackupDirectory))
            {
                Directory.CreateDirectory(_networkBackupDirectory);
            }
        }

        public string BackupDirectory => _backupDirectory;
        public string? NetworkBackupDirectory => _networkBackupDirectory;

        /// <summary>
        /// Inicia o agendamento de backup automático diário
        /// </summary>
        public void IniciarBackupAutomatico(int retencao = 10)
        {
            try
            {
                // Executa o primeiro backup imediatamente
                Task.Run(() => CriarBackupAutomaticoDiario(retencao));

                // Agenda o backup automático para cada 24 horas
                _backupAutomaticoTimer = new Timer(
                    callback: _ => CriarBackupAutomaticoDiario(retencao),
                    state: null,
                    dueTime: _intervaloBackupAutomatico,
                    period: _intervaloBackupAutomatico);

                _logger.LogInfo("Backup automático diário iniciado com intervalo de 24 horas.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao iniciar backup automático diário.", ex);
            }
        }

        /// <summary>
        /// Para o agendamento de backup automático
        /// </summary>
        public void PararBackupAutomatico()
        {
            try
            {
                _backupAutomaticoTimer?.Dispose();
                _backupAutomaticoTimer = null;
                _logger.LogInfo("Backup automático diário parado.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao parar backup automático diário.", ex);
            }
        }

        /// <summary>
        /// Executa backup automático ao encerrar a aplicação
        /// </summary>
        public void ExecutarBackupAoEncerrar(int retencao = 20)
        {
            try
            {
                PararBackupAutomatico();
                CriarBackupAoEncerrar(retencao);
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao executar backup ao encerrar.", ex);
            }
        }

        public string CriarBackupManual(string? destino = null)
        {
            return CriarBackup(destino, "manual");
        }

        public string CriarBackupAntesAtualizacao()
        {
            try
            {
                var caminhoBackup = CriarBackup(null, "pre_update");
                var verificado = VerificarBackup(caminhoBackup);

                if (!verificado)
                {
                    throw new InvalidOperationException("Backup de seguranca antes de atualizacao falhou na verificacao de integridade.");
                }

                _logger.LogInfo($"Backup antes de atualizacao criado e verificado: {caminhoBackup}");
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupAntesAtualizacao", "SQLite", BackupAuditId(caminhoBackup), BackupAuditDetails("Verificado=true", caminhoBackup));
                return caminhoBackup;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao criar backup antes de atualizacao.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Banco", "FalhaBackupAntesAtualizacao", ex, criticidade: "Critical");
                throw new InvalidOperationException(
                    "Nao foi possivel criar backup de seguranca. A atualizacao foi cancelada para proteger seus dados.", ex);
            }
        }

        public string CriarBackupAntesMigracao()
        {
            try
            {
                var caminhoBackup = CriarBackup(null, "pre_migration");
                var verificado = VerificarBackup(caminhoBackup);

                if (!verificado)
                {
                    throw new InvalidOperationException("Backup de seguranca antes de migracao falhou na verificacao de integridade.");
                }

                _logger.LogInfo($"Backup antes de migracao criado e verificado: {caminhoBackup}");
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupAntesMigracao", "SQLite", BackupAuditId(caminhoBackup), BackupAuditDetails("Verificado=true", caminhoBackup));
                return caminhoBackup;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao criar backup antes de migracao.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Banco", "FalhaBackupAntesMigracao", ex, criticidade: "Critical");
                throw new InvalidOperationException(
                    "Migracao sem backup valido e proibida. O backup de seguranca falhou.", ex);
            }
        }

        public string CriarBackupAntesOperacaoCritica(string operacao)
        {
            try
            {
                var caminhoBackup = CriarBackup(null, $"pre_{operacao.ToLower()}");
                var verificado = VerificarBackup(caminhoBackup);

                _logger.LogInfo($"Backup antes de operacao critica '{operacao}' criado: {caminhoBackup}, Verificado: {verificado}");
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", $"BackupAntes{operacao}", "SQLite", BackupAuditId(caminhoBackup), BackupAuditDetails($"Verificado={verificado}", caminhoBackup));
                return caminhoBackup;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao criar backup antes de operacao critica '{operacao}'.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Banco", $"FalhaBackupAntes{operacao}", ex);
                throw;
            }
        }

        public bool VerificarBackup(string caminhoBackup)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(caminhoBackup) || !File.Exists(caminhoBackup))
                {
                    _logger.LogWarning($"Backup nao encontrado: {caminhoBackup}");
                    return false;
                }

                if (IsSqlServerBackup(caminhoBackup))
                {
                    return VerificarBackupSqlServer(caminhoBackup);
                }

                var fileInfo = new FileInfo(caminhoBackup);
                if (fileInfo.Length == 0)
                {
                    _logger.LogWarning($"Backup com tamanho zero: {caminhoBackup}");
                    RegistrarHistoricoBackup(caminhoBackup, "verificacao", "Invalido", "Arquivo com tamanho zero");
                    return false;
                }

                var connectionString = new SqliteConnectionStringBuilder
                {
                    DataSource = caminhoBackup,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString();

                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA integrity_check;";
                var result = Convert.ToString(command.ExecuteScalar());
                var valido = string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);

                if (!valido)
                {
                    _logger.LogWarning($"Backup falhou na verificacao de integridade: {caminhoBackup}. Resultado: {result}");
                }

                RegistrarHistoricoBackup(caminhoBackup, "verificacao", valido ? "Verificado" : "Invalido", result ?? string.Empty);
                return valido;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao verificar backup '{caminhoBackup}'.", ex);
                RegistrarHistoricoBackup(caminhoBackup, "verificacao", "Erro", ex.Message);
                return false;
            }
        }

        public bool VerificarIntegridade(string caminhoBackup)
        {
            return VerificarBackup(caminhoBackup);
        }

        public BackupManifesto? LerManifesto(string caminhoBackup)
        {
            try
            {
                var caminhoManifesto = Path.ChangeExtension(caminhoBackup, ".json");
                if (!File.Exists(caminhoManifesto))
                {
                    return null;
                }

                var json = File.ReadAllText(caminhoManifesto);
                return JsonSerializer.Deserialize<BackupManifesto>(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao ler manifesto do backup '{caminhoBackup}': {ex.Message}");
                return null;
            }
        }

        public string RestaurarBackup(string caminhoBackup, bool criarBackupSeguranca = true)
        {
            if (!VerificarBackup(caminhoBackup))
            {
                throw new InvalidOperationException("Backup invalido ou corrompido.");
            }

            if (IsSqlServerBackup(caminhoBackup))
            {
                return RestaurarBackupSqlServer(caminhoBackup, criarBackupSeguranca);
            }

            string? backupSeguranca = null;
            if (criarBackupSeguranca)
            {
                try
                {
                    backupSeguranca = CriarBackup(null, "pre_restore");
                    _logger.LogInfo($"Backup de seguranca criado antes da restauracao: {backupSeguranca}");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Falha ao criar backup de seguranca antes da restauracao.", ex);
                    throw new InvalidOperationException(
                        "A restauracao substituira os dados atuais. Nao foi possivel criar um backup de seguranca. A operacao foi cancelada.", ex);
                }
            }

            string bancoDestino;
            using (var connection = _databaseService.GetSqliteConnection())
            {
                connection.Open();
                ExecutarCheckpoint(connection);
                bancoDestino = connection.DataSource;
            }

            SqliteConnection.ClearAllPools();
            File.Copy(caminhoBackup, bancoDestino, overwrite: true);
            RemoverArquivoSeExistir(bancoDestino + "-wal");
            RemoverArquivoSeExistir(bancoDestino + "-shm");

            RegistrarHistoricoBackup(caminhoBackup, "restore", "Restaurado", backupSeguranca ?? string.Empty);
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupRestaurado", "SQLite", BackupAuditId(caminhoBackup), BackupAuditDetails($"BackupSeguranca={backupSeguranca}", caminhoBackup));

            return backupSeguranca ?? string.Empty;
        }

        private bool VerificarBackupSqlServer(string caminhoBackup)
        {
            try
            {
                using var connection = _databaseService.GetConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandTimeout = 600;
                command.CommandText = "RESTORE VERIFYONLY FROM DISK = @Backup WITH CHECKSUM;";
                command.Parameters.AddWithValue("@Backup", Path.GetFullPath(caminhoBackup));
                command.ExecuteNonQuery();

                RegistrarHistoricoBackup(caminhoBackup, "verificacao", "Verificado", "RESTORE VERIFYONLY concluiu com sucesso.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao verificar backup SQL Server '{caminhoBackup}'.", ex);
                RegistrarHistoricoBackup(caminhoBackup, "verificacao", "Erro", ex.Message);
                return false;
            }
        }

        private string RestaurarBackupSqlServer(string caminhoBackup, bool criarBackupSeguranca)
        {
            string? backupSeguranca = null;
            if (criarBackupSeguranca)
            {
                try
                {
                    backupSeguranca = CriarBackup(null, "pre_restore");
                    _logger.LogInfo($"Backup SQL Server de seguranca criado antes da restauracao: {backupSeguranca}");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Falha ao criar backup SQL Server de seguranca antes da restauracao.", ex);
                    throw new InvalidOperationException(
                        "A restauracao substituira os dados atuais. Nao foi possivel criar um backup de seguranca. A operacao foi cancelada.", ex);
                }
            }

            string targetDatabase;
            string targetConnectionString;
            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                targetDatabase = connection.Database;
                targetConnectionString = connection.ConnectionString;
            }

            var masterConnectionString = BuildMasterConnectionString(targetConnectionString);
            SqlConnection.ClearAllPools();

            using var masterConnection = new SqlConnection(masterConnectionString);
            masterConnection.Open();

            try
            {
                using var command = masterConnection.CreateCommand();
                command.CommandTimeout = 600;
                command.CommandText = $@"
                    ALTER DATABASE {QuoteSqlIdentifier(targetDatabase)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    RESTORE DATABASE {QuoteSqlIdentifier(targetDatabase)}
                    FROM DISK = @Backup
                    WITH REPLACE, CHECKSUM;
                    ALTER DATABASE {QuoteSqlIdentifier(targetDatabase)} SET MULTI_USER;";
                command.Parameters.AddWithValue("@Backup", Path.GetFullPath(caminhoBackup));
                command.ExecuteNonQuery();
            }
            catch
            {
                TrySetSqlServerMultiUser(masterConnection, targetDatabase);
                throw;
            }

            RegistrarHistoricoBackup(caminhoBackup, "restore", "Restaurado", backupSeguranca ?? string.Empty);
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupRestaurado", "SQL Server", BackupAuditId(caminhoBackup), BackupAuditDetails($"BackupSeguranca={backupSeguranca}", caminhoBackup));

            return backupSeguranca ?? string.Empty;
        }

        public string? CriarBackupAutomaticoDiario(int retencao = 10)
        {
            try
            {
                var marcadorHoje = DateTime.Today.ToString("yyyy-MM-dd");
                var backupExistente = Directory
                    .EnumerateFiles(_backupDirectory, $"PrimoAutoEletrica_Backup_{marcadorHoje}_*.db")
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(backupExistente))
                {
                    return backupExistente;
                }

                var caminho = CriarBackup(null, "auto");
                AplicarRetencao("PrimoAutoEletrica_Backup_*.db", retencao);
                return caminho;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao criar backup automatico diario.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Banco", "FalhaBackupAutomatico", ex);

                ShellNotificationService.Publish(new ShellNotificationRequest
                {
                    Title = UiText.T("BackupDailyFailed"),
                    Message = "O backup automatico diario falhou. Verifique o armazenamento ou permissoes da pasta.",
                    Details = ex.Message,
                    Type = ShellNotificationType.Error,
                    Source = "BackupService"
                });

                return null;
            }
        }

        public string? CriarBackupAoEncerrar(int retencao = 20)
        {
            try
            {
                var caminho = CriarBackup(null, "close");
                AplicarRetencao("PrimoAutoEletrica_Backup_*.db", retencao);
                return caminho;
            }
            catch (Exception ex)
            {
                _logger.LogError("Falha ao criar backup automatico no encerramento.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Banco", "FalhaBackupEncerramento", ex);

                ShellNotificationService.Publish(new ShellNotificationRequest
                {
                    Title = UiText.T("BackupExitFailed"),
                    Message = "O backup automatico ao fechar o sistema falhou. Verifique o armazenamento ou permissoes da pasta.",
                    Details = ex.Message,
                    Type = ShellNotificationType.Warning,
                    Source = "BackupService"
                });

                return null;
            }
        }

        private string CriarBackup(string? destino, string origem)
        {
            if (IsSqlServerRuntime())
            {
                return CriarBackupSqlServer(destino, origem);
            }

            var inicio = DateTime.Now;

            using var connection = _databaseService.GetSqliteConnection();
            connection.Open();

            ExecutarCheckpoint(connection);

            var bancoOrigem = connection.DataSource;
            if (string.IsNullOrWhiteSpace(bancoOrigem) || !File.Exists(bancoOrigem))
            {
                throw new FileNotFoundException("Banco SQLite de origem nao localizado.", bancoOrigem);
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var caminhoDestino = string.IsNullOrWhiteSpace(destino)
                ? Path.Combine(_backupDirectory, $"PrimoAutoEletrica_Backup_{timestamp}.db")
                : destino;

            var pastaDestino = Path.GetDirectoryName(caminhoDestino);
            if (!string.IsNullOrWhiteSpace(pastaDestino))
            {
                Directory.CreateDirectory(pastaDestino);
            }

            File.Copy(bancoOrigem, caminhoDestino, overwrite: true);

            var fileInfo = new FileInfo(caminhoDestino);
            var hashSha256 = CalcularSha256(caminhoDestino);
            var versao = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev";
            var usuario = global::PrimoAutoEletrica.App.Session.IsAuthenticated
                ? global::PrimoAutoEletrica.App.Session.UserName
                : "Nao autenticado";

            var manifesto = new BackupManifesto
            {
                DataHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Usuario = usuario,
                TipoBackup = origem,
                CaminhoOriginalBanco = bancoOrigem,
                TamanhoBytes = fileInfo.Length,
                HashSha256 = hashSha256,
                VersaoSistema = versao,
                Ambiente = global::PrimoAutoEletrica.App.RuntimeModeName,
                Computador = Environment.MachineName,
                ResultadoVerificacao = "Pendente"
            };

            var caminhoManifesto = Path.ChangeExtension(caminhoDestino, ".json");
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(caminhoManifesto, JsonSerializer.Serialize(manifesto, jsonOptions));

            var duracao = (DateTime.Now - inicio).TotalMilliseconds;
            _logger.LogInfo($"Backup do banco criado em '{caminhoDestino}' em {duracao:F0}ms. Hash: {hashSha256}");
            RegistrarHistoricoBackup(caminhoDestino, origem, "Criado", string.Empty);
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupCriado", "SQLite", BackupAuditId(caminhoDestino), BackupAuditDetails($"Origem={origem};Tamanho={fileInfo.Length};Duracao={duracao:F0}ms", caminhoDestino));

            if (!string.IsNullOrWhiteSpace(_networkBackupDirectory))
            {
                CopiarParaRede(caminhoDestino, caminhoManifesto);
            }

            return caminhoDestino;
        }

        private string CriarBackupSqlServer(string? destino, string origem)
        {
            var inicio = DateTime.Now;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var caminhoDestino = string.IsNullOrWhiteSpace(destino)
                ? Path.Combine(_backupDirectory, $"PrimoAutoEletrica_Backup_{timestamp}.bak")
                : destino;

            var pastaDestino = Path.GetDirectoryName(caminhoDestino);
            if (!string.IsNullOrWhiteSpace(pastaDestino))
            {
                Directory.CreateDirectory(pastaDestino);
            }

            caminhoDestino = Path.GetFullPath(caminhoDestino);

            using (var connection = _databaseService.GetConnection())
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandTimeout = 600;
                command.CommandText = $@"
                    BACKUP DATABASE {QuoteSqlIdentifier(connection.Database)}
                    TO DISK = @Destino
                    WITH INIT, CHECKSUM;";
                command.Parameters.AddWithValue("@Destino", caminhoDestino);
                command.ExecuteNonQuery();
            }

            var fileInfo = new FileInfo(caminhoDestino);
            var hashSha256 = CalcularSha256(caminhoDestino);
            var versao = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev";
            var usuario = global::PrimoAutoEletrica.App.Session.IsAuthenticated
                ? global::PrimoAutoEletrica.App.Session.UserName
                : "Nao autenticado";

            var manifesto = new BackupManifesto
            {
                DataHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Usuario = usuario,
                TipoBackup = origem,
                CaminhoOriginalBanco = _databaseService.DatabasePath,
                TamanhoBytes = fileInfo.Length,
                HashSha256 = hashSha256,
                VersaoSistema = versao,
                Ambiente = global::PrimoAutoEletrica.App.RuntimeModeName,
                Computador = Environment.MachineName,
                ResultadoVerificacao = "Pendente"
            };

            var caminhoManifesto = Path.ChangeExtension(caminhoDestino, ".json");
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(caminhoManifesto, JsonSerializer.Serialize(manifesto, jsonOptions));

            var duracao = (DateTime.Now - inicio).TotalMilliseconds;
            _logger.LogInfo($"Backup SQL Server criado em '{caminhoDestino}' em {duracao:F0}ms. Hash: {hashSha256}");
            RegistrarHistoricoBackup(caminhoDestino, origem, "Criado", "SQL Server BACKUP DATABASE concluido.");
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupCriado", "SQL Server", BackupAuditId(caminhoDestino), BackupAuditDetails($"Origem={origem};Tamanho={fileInfo.Length};Duracao={duracao:F0}ms", caminhoDestino));

            if (!string.IsNullOrWhiteSpace(_networkBackupDirectory))
            {
                CopiarParaRede(caminhoDestino, caminhoManifesto);
            }

            return caminhoDestino;
        }

        private void CopiarParaRede(string caminhoBackup, string caminhoManifesto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_networkBackupDirectory))
                {
                    return;
                }

                var nomeArquivo = Path.GetFileName(caminhoBackup);
                var destinoBackup = Path.Combine(_networkBackupDirectory, nomeArquivo);
                var destinoManifesto = Path.Combine(_networkBackupDirectory, Path.GetFileName(caminhoManifesto));

                File.Copy(caminhoBackup, destinoBackup, overwrite: true);
                File.Copy(caminhoManifesto, destinoManifesto, overwrite: true);

                _logger.LogInfo($"Backup copiado para rede: {destinoBackup}");
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupCopiadoRede", "SQLite", BackupAuditId(destinoBackup), BackupAuditDetails("Sucesso=true", destinoBackup));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao copiar backup para rede '{_networkBackupDirectory}'.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Banco", "FalhaCopiaRede", ex);

                ShellNotificationService.Publish(new ShellNotificationRequest
                {
                    Title = UiText.T("BackupNetworkFailed"),
                    Message = $"O backup nao pôde ser copiado para a pasta de rede '{_networkBackupDirectory}'. O backup local foi mantido.",
                    Details = ex.Message,
                    Type = ShellNotificationType.Warning,
                    Source = "BackupService"
                });
            }
        }

        private static void ExecutarCheckpoint(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(FULL);";
            command.ExecuteNonQuery();
        }

        private void AplicarRetencao(string padraoBusca, int retencao)
        {
            if (retencao <= 0)
            {
                return;
            }

            var backups = Directory
                .EnumerateFiles(_backupDirectory, padraoBusca)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.CreationTimeUtc)
                .ToList();

            foreach (var backup in backups.Skip(retencao))
            {
                try
                {
                    backup.Delete();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Falha ao remover backup antigo '{backup.FullName}': {ex.Message}");
                }
            }
        }

        private void RegistrarHistoricoBackup(string caminho, string tipo, string status, string mensagem)
        {
            try
            {
                if (IsSqlServerRuntime())
                {
                    RegistrarHistoricoBackupSqlServer(caminho, tipo, status, mensagem);
                    return;
                }

                using var connection = _databaseService.GetSqliteConnection();
                connection.Open();

                var fileInfo = File.Exists(caminho) ? new FileInfo(caminho) : null;
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO DatabaseBackups
                    (
                        Id,
                        Caminho,
                        Tipo,
                        TamanhoBytes,
                        CriadoEm,
                        VerificadoEm,
                        HashSha256,
                        Status,
                        Mensagem
                    )
                    VALUES
                    (
                        @Id,
                        @Caminho,
                        @Tipo,
                        @TamanhoBytes,
                        @CriadoEm,
                        @VerificadoEm,
                        @HashSha256,
                        @Status,
                        @Mensagem
                    );";
                command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
                command.Parameters.AddWithValue("@Caminho", caminho);
                command.Parameters.AddWithValue("@Tipo", tipo);
                command.Parameters.AddWithValue("@TamanhoBytes", fileInfo?.Length ?? 0);
                command.Parameters.AddWithValue("@CriadoEm", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@VerificadoEm", status is "Verificado" or "Invalido" or "Erro"
                    ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    : DBNull.Value);
                command.Parameters.AddWithValue("@HashSha256", fileInfo == null ? DBNull.Value : CalcularSha256(caminho));
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@Mensagem", string.IsNullOrWhiteSpace(mensagem) ? DBNull.Value : mensagem);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Falha ao registrar historico de backup '{caminho}': {ex.Message}");
            }
        }

        private void RegistrarHistoricoBackupSqlServer(string caminho, string tipo, string status, string mensagem)
        {
            using var connection = _databaseService.GetConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO DatabaseBackups
                (
                    Id,
                    CaminhoArquivo,
                    Tipo,
                    Status,
                    Detalhes,
                    CriadoEm
                )
                VALUES
                (
                    @Id,
                    @CaminhoArquivo,
                    @Tipo,
                    @Status,
                    @Detalhes,
                    @CriadoEm
                );";
            command.Parameters.AddWithValue("@Id", Guid.NewGuid());
            command.Parameters.AddWithValue("@CaminhoArquivo", caminho);
            command.Parameters.AddWithValue("@Tipo", tipo);
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@Detalhes", string.IsNullOrWhiteSpace(mensagem) ? DBNull.Value : mensagem);
            command.Parameters.AddWithValue("@CriadoEm", DateTime.Now);
            command.ExecuteNonQuery();
        }

        private static string CalcularSha256(string caminho)
        {
            using var stream = File.OpenRead(caminho);
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return Convert.ToHexString(SHA256.HashData(bytes));
        }

        private static string BackupAuditId(string caminho)
        {
            var fileName = Path.GetFileName(caminho);
            return string.IsNullOrWhiteSpace(fileName) ? "backup" : fileName;
        }

        private static string BackupAuditDetails(string details, string caminho)
        {
            return $"{details};Caminho={caminho}";
        }

        private bool IsSqlServerRuntime()
        {
            return string.Equals(_databaseService.RuntimeProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsSqlServerBackup(string caminhoBackup)
        {
            return IsSqlServerRuntime() ||
                   string.Equals(Path.GetExtension(caminhoBackup), ".bak", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildMasterConnectionString(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                InitialCatalog = "master"
            };
            return builder.ConnectionString;
        }

        private static string QuoteSqlIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new InvalidOperationException("Nome do banco SQL Server nao foi identificado.");
            }

            return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
        }

        private static void TrySetSqlServerMultiUser(SqlConnection connection, string databaseName)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandTimeout = 120;
                command.CommandText = $"ALTER DATABASE {QuoteSqlIdentifier(databaseName)} SET MULTI_USER;";
                command.ExecuteNonQuery();
            }
            catch
            {
                // A excecao original da restauracao e mais importante para diagnostico.
            }
        }

        private static void RemoverArquivoSeExistir(string caminho)
        {
            if (File.Exists(caminho))
            {
                File.Delete(caminho);
            }
        }
    }

    public class BackupManifesto
    {
        public string DataHora { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string TipoBackup { get; set; } = string.Empty;
        public string CaminhoOriginalBanco { get; set; } = string.Empty;
        public long TamanhoBytes { get; set; }
        public string HashSha256 { get; set; } = string.Empty;
        public string VersaoSistema { get; set; } = string.Empty;
        public string Ambiente { get; set; } = string.Empty;
        public string Computador { get; set; } = string.Empty;
        public string ResultadoVerificacao { get; set; } = string.Empty;
    }
}
