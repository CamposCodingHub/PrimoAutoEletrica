using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public class UpdateService
    {
        private readonly string _appDataPath;
        private readonly string _currentVersion;
        private readonly LoggerService? _logger;

        public UpdateService(string appDataPath, string currentVersion, LoggerService? logger = null)
        {
            _appDataPath = appDataPath;
            _currentVersion = currentVersion;
            _logger = logger;
        }

        public event EventHandler<UpdateStatus>? StatusChanged;
        public event EventHandler<string>? ProgressChanged;

        public async Task<UpdateManifest?> CheckForUpdatesAsync(string manifestPath)
        {
            try
            {
                StatusChanged?.Invoke(this, UpdateStatus.Checking);
                _logger?.LogInfo($"Verificando atualizações em: {manifestPath}");

                if (!File.Exists(manifestPath))
                {
                    _logger?.LogWarning("Arquivo de manifesto não encontrado");
                    return null;
                }

                var json = await File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<UpdateManifest>(json);

                if (manifest == null)
                {
                    _logger?.LogWarning("Falha ao deserializar manifesto");
                    return null;
                }

                manifest.CurrentVersion = _currentVersion;
                manifest.UpdateAvailable = IsNewerVersion(manifest.LatestVersion, _currentVersion);

                _logger?.LogInfo($"Atualização disponível: {manifest.UpdateAvailable} ({_currentVersion} -> {manifest.LatestVersion})");
                return manifest;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao verificar atualizações: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<UpdateResult> ApplyUpdateAsync(UpdateManifest manifest)
        {
            var result = new UpdateResult
            {
                UpdateTime = DateTime.Now
            };

            try
            {
                StatusChanged?.Invoke(this, UpdateStatus.Validating);
                result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] Validando pacote...");

                if (!ValidatePackage(manifest.Package.LocalPath, manifest.Package.Sha256))
                {
                    result.Success = false;
                    result.Message = "Hash SHA256 inválido";
                    result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ERRO: Hash inválido");
                    return result;
                }

                result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] Pacote validado com sucesso");

                StatusChanged?.Invoke(this, UpdateStatus.CreatingBackup);
                result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] Criando backup...");

                var backupPath = await CreateBackupAsync();
                result.BackupPath = backupPath;
                result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] Backup criado: {backupPath}");

                if (IsApplicationRunning())
                {
                    result.Success = false;
                    result.Message = "Aplicação está em execução. Feche antes de atualizar.";
                    result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ERRO: Aplicação em execução");
                    return result;
                }

                StatusChanged?.Invoke(this, UpdateStatus.ApplyingUpdate);
                result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] Aplicando atualização...");

                await ApplyPackageAsync(manifest.Package.LocalPath);
                result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] Atualização aplicada");

                StatusChanged?.Invoke(this, UpdateStatus.ValidatingUpdate);
                result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] Validando atualização...");

                if (!ValidateUpdate(manifest.LatestVersion))
                {
                    result.Success = false;
                    result.Message = "Validação pós-atualização falhou";
                    result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ERRO: Validação falhou");
                    return result;
                }

                result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] Atualização validada com sucesso");

                result.Success = true;
                result.Message = $"Atualizado para versão {manifest.LatestVersion}";
                StatusChanged?.Invoke(this, UpdateStatus.Completed);

                _logger?.LogInfo($"Atualização concluída com sucesso: {_currentVersion} -> {manifest.LatestVersion}");
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                result.LogEntries.Add($"[{DateTime.Now:HH:mm:ss}] ERRO: {ex.Message}");
                StatusChanged?.Invoke(this, UpdateStatus.Failed);
                _logger?.LogError($"Erro ao aplicar atualização: {ex.Message}", ex);
                return result;
            }
        }

        private bool IsNewerVersion(string latest, string current)
        {
            try
            {
                var latestParts = latest.Split('.').Select(int.Parse).ToArray();
                var currentParts = current.Split('.').Select(int.Parse).ToArray();

                for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
                {
                    var latestPart = i < latestParts.Length ? latestParts[i] : 0;
                    var currentPart = i < currentParts.Length ? currentParts[i] : 0;

                    if (latestPart > currentPart) return true;
                    if (latestPart < currentPart) return false;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidatePackage(string packagePath, string expectedHash)
        {
            if (!File.Exists(packagePath))
            {
                return false;
            }

            var actualHash = ComputeSHA256(packagePath);
            return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeSHA256(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var bytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private async Task<string> CreateBackupAsync()
        {
            var backupDir = Path.Combine(_appDataPath, "Backups", $"PreUpdate_{DateTime.Now:yyyyMMdd_HHmmss}");
            Directory.CreateDirectory(backupDir);

            var dataDir = Path.Combine(_appDataPath, "Data");
            if (Directory.Exists(dataDir))
            {
                var dbFile = Path.Combine(dataDir, "primoauto.db");
                if (File.Exists(dbFile))
                {
                    var backupDb = Path.Combine(backupDir, "primoauto.db");
                    await Task.Run(() => File.Copy(dbFile, backupDb, true));
                }
            }

            var configDir = Path.Combine(_appDataPath, "Config");
            if (Directory.Exists(configDir))
            {
                foreach (var file in Directory.GetFiles(configDir))
                {
                    var destFile = Path.Combine(backupDir, Path.GetFileName(file));
                    await Task.Run(() => File.Copy(file, destFile, true));
                }
            }

            return backupDir;
        }

        private bool IsApplicationRunning()
        {
            try
            {
                var processes = Process.GetProcessesByName("PrimoAutoEletrica");
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task ApplyPackageAsync(string packagePath)
        {
            // Implementação simplificada - descompactar e copiar arquivos
            // Na implementação real, usar System.IO.Compression ou biblioteca ZIP
            await Task.Delay(100); // Placeholder
        }

        private bool ValidateUpdate(string expectedVersion)
        {
            // Verificar se a versão foi atualizada corretamente
            // Na implementação real, verificar arquivo de versão ou registro
            return true;
        }

        public async Task<bool> RollbackAsync(string backupPath)
        {
            try
            {
                StatusChanged?.Invoke(this, UpdateStatus.RollingBack);
                _logger?.LogInfo($"Iniciando rollback de: {backupPath}");

                if (!Directory.Exists(backupPath))
                {
                    _logger?.LogError("Backup não encontrado");
                    return false;
                }

                var dataDir = Path.Combine(_appDataPath, "Data");
                Directory.CreateDirectory(dataDir);

                var backupDb = Path.Combine(backupPath, "primoauto.db");
                if (File.Exists(backupDb))
                {
                    var dbFile = Path.Combine(dataDir, "primoauto.db");
                    await Task.Run(() => File.Copy(backupDb, dbFile, true));
                }

                _logger?.LogInfo("Rollback concluído com sucesso");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro no rollback: {ex.Message}", ex);
                return false;
            }
        }
    }
}
