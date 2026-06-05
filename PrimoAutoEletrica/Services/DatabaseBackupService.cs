using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Reflection;

namespace PrimoAutoEletrica.Services
{
    public class DatabaseBackupService
    {
        private readonly DatabaseService _databaseService;
        private readonly LoggerService _logger;
        private readonly string _backupDirectory;
        private readonly string? _networkBackupDirectory;

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
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupAntesAtualizacao", "SQLite", caminhoBackup, "Verificado=true");
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
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupAntesMigracao", "SQLite", caminhoBackup, "Verificado=true");
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
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", $"BackupAntes{operacao}", "SQLite", caminhoBackup, $"Verificado={verificado}");
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
            using (var connection = _databaseService.GetConnection())
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
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupRestaurado", "SQLite", caminhoBackup, $"BackupSeguranca={backupSeguranca}");

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
                    Title = "Falha no backup diario",
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
                    Title = "Falha no backup ao encerrar",
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
            var inicio = DateTime.Now;

            using var connection = _databaseService.GetConnection();
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
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupCriado", "SQLite", caminhoDestino, $"Origem={origem};Tamanho={fileInfo.Length};Duracao={duracao:F0}ms");

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
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica("Banco", "BackupCopiadoRede", "SQLite", destinoBackup, "Sucesso=true");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Falha ao copiar backup para rede '{_networkBackupDirectory}'.", ex);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Banco", "FalhaCopiaRede", ex);

                ShellNotificationService.Publish(new ShellNotificationRequest
                {
                    Title = "Falha na copia para rede",
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
                using var connection = _databaseService.GetConnection();
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

        private static string CalcularSha256(string caminho)
        {
            using var stream = File.OpenRead(caminho);
            return Convert.ToHexString(SHA256.HashData(stream));
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
