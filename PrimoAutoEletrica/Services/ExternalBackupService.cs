using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public class ExternalBackupService
    {
        private readonly string _appDataPath;
        private readonly LoggerService? _logger;
        private readonly BackupSettings _settings;

        public ExternalBackupService(string appDataPath, BackupSettings settings, LoggerService? logger = null)
        {
            _appDataPath = appDataPath;
            _settings = settings;
            _logger = logger;
        }

        public async Task<BackupResult> CreateBackupAsync(string description = "", bool manual = false)
        {
            var result = new BackupResult
            {
                BackupTime = DateTime.Now
            };

            try
            {
                var backupId = $"{(manual ? "Manual" : "Auto")}_{DateTime.Now:yyyyMMdd_HHmmss}";
                var backupDir = Path.Combine(_appDataPath, "Backups", backupId);
                Directory.CreateDirectory(backupDir);

                _logger?.LogInfo($"Iniciando backup: {backupId}");

                // Copiar banco de dados
                var dataDir = Path.Combine(_appDataPath, "Data");
                if (Directory.Exists(dataDir))
                {
                    var dbFile = Path.Combine(dataDir, "primoauto.db");
                    if (File.Exists(dbFile))
                    {
                        var backupDb = Path.Combine(backupDir, "primoauto.db");
                        await Task.Run(() => File.Copy(dbFile, backupDb, true));
                        result.OriginalSize += new FileInfo(dbFile).Length;
                    }
                }

                // Copiar configurações
                var configDir = Path.Combine(_appDataPath, "Config");
                if (Directory.Exists(configDir))
                {
                    foreach (var file in Directory.GetFiles(configDir))
                    {
                        var destFile = Path.Combine(backupDir, Path.GetFileName(file));
                        await Task.Run(() => File.Copy(file, destFile, true));
                        result.OriginalSize += new FileInfo(file).Length;
                    }
                }

                // Copiar mídia se configurado
                if (_settings.AutoBackup.IncludeMedia)
                {
                    var mediaDir = Path.Combine(_appDataPath, "Media");
                    if (Directory.Exists(mediaDir))
                    {
                        var backupMedia = Path.Combine(backupDir, "Media");
                        CopyDirectory(mediaDir, backupMedia);
                        result.OriginalSize += GetDirectorySize(mediaDir);
                    }
                }

                // Compactar se configurado
                if (_settings.Compression.Enabled)
                {
                    var zipPath = Path.Combine(_appDataPath, "Backups", $"{backupId}.zip");
                    await CreateZipAsync(backupDir, zipPath, _settings.Compression.Password);
                    
                    // Remover diretório não compactado
                    Directory.Delete(backupDir, true);
                    
                    result.BackupPath = zipPath;
                    result.CompressedSize = new FileInfo(zipPath).Length;
                    result.CompressionRatio = (double)result.CompressedSize / result.OriginalSize;
                }
                else
                {
                    result.BackupPath = backupDir;
                    result.CompressedSize = result.OriginalSize;
                    result.CompressionRatio = 1.0;
                }

                // Calcular hash
                var hash = ComputeSHA256(result.BackupPath);

                // Criar info do backup
                result.BackupInfo = new BackupInfo
                {
                    Id = backupId,
                    Type = manual ? "manual" : "auto",
                    CreatedAt = result.BackupTime,
                    Size = result.CompressedSize,
                    Path = result.BackupPath,
                    Sha256 = hash,
                    Compressed = _settings.Compression.Enabled,
                    IncludesMedia = _settings.AutoBackup.IncludeMedia,
                    IncludesLogs = _settings.AutoBackup.IncludeLogs,
                    Description = description
                };

                // Copiar para destino externo se configurado
                if (_settings.ExternalBackup.Enabled)
                {
                    await CopyToExternalAsync(result.BackupPath);
                }

                result.Success = true;
                result.Message = $"Backup criado com sucesso: {FormatSize(result.CompressedSize)}";

                _logger?.LogInfo($"Backup concluído: {backupId} ({FormatSize(result.CompressedSize)})");
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                _logger?.LogError($"Erro ao criar backup: {ex.Message}", ex);
                return result;
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            try
            {
                _logger?.LogInfo($"Iniciando restauração: {backupPath}");

                // Validar backup
                if (!File.Exists(backupPath))
                {
                    _logger?.LogError("Arquivo de backup não encontrado");
                    return false;
                }

                // Descompactar se necessário
                string extractPath;
                if (backupPath.EndsWith(".zip"))
                {
                    extractPath = Path.Combine(_appDataPath, "Temp", $"Restore_{DateTime.Now:yyyyMMdd_HHmmss}");
                    Directory.CreateDirectory(extractPath);
                    
                    await ExtractZipAsync(backupPath, extractPath, _settings.Compression.Password);
                }
                else
                {
                    extractPath = backupPath;
                }

                // Restaurar banco de dados
                var backupDb = Path.Combine(extractPath, "primoauto.db");
                if (File.Exists(backupDb))
                {
                    var dataDir = Path.Combine(_appDataPath, "Data");
                    Directory.CreateDirectory(dataDir);
                    var targetDb = Path.Combine(dataDir, "primoauto.db");
                    
                    // Backup do estado atual
                    if (File.Exists(targetDb))
                    {
                        var currentBackup = Path.Combine(_appDataPath, "Backups", $"PreRestore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                        File.Copy(targetDb, currentBackup, true);
                    }
                    
                    await Task.Run(() => File.Copy(backupDb, targetDb, true));
                }

                // Restaurar configurações
                var configDir = Path.Combine(_appDataPath, "Config");
                Directory.CreateDirectory(configDir);
                
                foreach (var file in Directory.GetFiles(extractPath, "*.json"))
                {
                    var destFile = Path.Combine(configDir, Path.GetFileName(file));
                    await Task.Run(() => File.Copy(file, destFile, true));
                }

                // Restaurar mídia se existir
                var backupMedia = Path.Combine(extractPath, "Media");
                if (Directory.Exists(backupMedia))
                {
                    var mediaDir = Path.Combine(_appDataPath, "Media");
                    CopyDirectory(backupMedia, mediaDir);
                }

                // Limpar temporário
                if (backupPath.EndsWith(".zip"))
                {
                    Directory.Delete(extractPath, true);
                }

                _logger?.LogInfo("Restauração concluída com sucesso");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao restaurar backup: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> CopyToExternalAsync(string backupPath)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.ExternalBackup.Destination))
                {
                    _logger?.LogWarning("Destino externo não configurado");
                    return false;
                }

                var destDir = _settings.ExternalBackup.Destination;
                Directory.CreateDirectory(destDir);

                var fileName = Path.GetFileName(backupPath);
                var destPath = Path.Combine(destDir, fileName);

                await Task.Run(() => File.Copy(backupPath, destPath, true));

                // Copiar hash
                var hashFile = Path.ChangeExtension(backupPath, ".sha256");
                if (File.Exists(hashFile))
                {
                    var destHash = Path.Combine(destDir, Path.GetFileName(hashFile));
                    File.Copy(hashFile, destHash, true);
                }

                _logger?.LogInfo($"Backup copiado para destino externo: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao copiar para destino externo: {ex.Message}", ex);
                return false;
            }
        }

        public void ApplyRetentionPolicy()
        {
            try
            {
                var backupDir = Path.Combine(_appDataPath, "Backups");
                if (!Directory.Exists(backupDir))
                {
                    return;
                }

                var now = DateTime.Now;
                var backups = Directory.GetDirectories(backupDir)
                    .Select(d => new DirectoryInfo(d))
                    .OrderByDescending(d => d.CreationTime)
                    .ToList();

                // Aplicar política de retenção
                var toDelete = new List<DirectoryInfo>();

                // Backups diários
                var dailyBackups = backups.Where(b => b.Name.StartsWith("Daily_")).ToList();
                if (dailyBackups.Count > _settings.Retention.DailyBackups)
                {
                    toDelete.AddRange(dailyBackups.Skip(_settings.Retention.DailyBackups));
                }

                // Backups semanais
                var weeklyBackups = backups.Where(b => b.Name.StartsWith("Weekly_")).ToList();
                if (weeklyBackups.Count > _settings.Retention.WeeklyBackups)
                {
                    toDelete.AddRange(weeklyBackups.Skip(_settings.Retention.WeeklyBackups));
                }

                // Backups de atualização
                var updateBackups = backups.Where(b => b.Name.StartsWith("PreUpdate_")).ToList();
                if (updateBackups.Count > _settings.Retention.PreUpdateBackups)
                {
                    toDelete.AddRange(updateBackups.Skip(_settings.Retention.PreUpdateBackups));
                }

                // Remover backups antigos
                foreach (var backup in toDelete)
                {
                    try
                    {
                        Directory.Delete(backup.FullName, true);
                        _logger?.LogInfo($"Backup removido pela política de retenção: {backup.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Erro ao remover backup {backup.Name}: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Erro ao aplicar política de retenção: {ex.Message}", ex);
            }
        }

        private async Task CreateZipAsync(string sourceDir, string zipPath, string? password = null)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(password))
                {
                    ZipFile.CreateFromDirectory(sourceDir, zipPath, CompressionLevel.Optimal, false);
                }
                else
                {
                    // Criar ZIP com senha (requer implementação adicional)
                    ZipFile.CreateFromDirectory(sourceDir, zipPath, CompressionLevel.Optimal, false);
                }
            });
        }

        private async Task ExtractZipAsync(string zipPath, string extractPath, string? password = null)
        {
            await Task.Run(() =>
            {
                if (string.IsNullOrEmpty(password))
                {
                    ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                }
                else
                {
                    // Extrair ZIP com senha (requer implementação adicional)
                    ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                }
            });
        }

        private void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            
            foreach (var file in Directory.GetFiles(source))
            {
                var destFile = Path.Combine(destination, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            foreach (var dir in Directory.GetDirectories(source))
            {
                var destDir = Path.Combine(destination, Path.GetFileName(dir));
                CopyDirectory(dir, destDir);
            }
        }

        private long GetDirectorySize(string path)
        {
            long size = 0;
            
            try
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    size += new FileInfo(file).Length;
                }

                foreach (var dir in Directory.GetDirectories(path))
                {
                    size += GetDirectorySize(dir);
                }
            }
            catch
            {
                // Ignorar erros de acesso
            }

            return size;
        }

        private string ComputeSHA256(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var bytes = sha256.ComputeHash(stream);
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
