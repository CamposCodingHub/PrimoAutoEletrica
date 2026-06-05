using System;
using System.IO;
using System.Text.Json;
using PrimoAutoEletrica.Services.DatabaseProviders;

namespace PrimoAutoEletrica.Services
{
    public sealed class DatabaseConnectionSettings
    {
        public const int DefaultSessionInactivityTimeoutMinutes = 30;
        public const int MinimumSessionInactivityTimeoutMinutes = 5;
        public const int MaximumSessionInactivityTimeoutMinutes = 480;
        public const int DefaultCommandTimeoutSeconds = 30;
        public const int MinimumCommandTimeoutSeconds = 5;
        public const int MaximumCommandTimeoutSeconds = 600;

        public string Provider { get; set; } = "SQLite";
        public string SQLitePath { get; set; } = "primoauto.db";
        public string SqlServerHost { get; set; } = ".\\SQLEXPRESS";
        public string? SqlServerInstance { get; set; }
        public string SqlServerDatabase { get; set; } = "PrimoAutoEletrica";
        public bool UseWindowsAuthentication { get; set; } = true;
        public string SqlUser { get; set; } = string.Empty;
        public string SqlPasswordProtected { get; set; } = string.Empty;
        public string? SqlServerUsername { get; set; }
        public string? SqlServerPassword { get; set; }
        public bool EncryptSqlServerConnection { get; set; } = true;
        public bool TrustSqlServerCertificate { get; set; } = true;
        public int CommandTimeoutSeconds { get; set; } = DefaultCommandTimeoutSeconds;
        public int SessionInactivityTimeoutMinutes { get; set; } = DefaultSessionInactivityTimeoutMinutes;
        public string? NetworkBackupDirectory { get; set; }

        public string ResolveSqlitePath(string appDataPath)
        {
            if (Path.IsPathRooted(SQLitePath))
            {
                return SQLitePath;
            }

            return Path.Combine(appDataPath, SQLitePath);
        }

        public string BuildSqlServerConnectionString(string? unprotectedPassword = null)
        {
            var authentication = UseWindowsAuthentication
                ? "Integrated Security=True"
                : $"User ID={SqlUser};Password={unprotectedPassword ?? string.Empty}";

            return $"Server={SqlServerHost};Database={SqlServerDatabase};{authentication};Encrypt={EncryptSqlServerConnection};TrustServerCertificate={TrustSqlServerCertificate};Connect Timeout={CommandTimeoutSeconds};";
        }

        public bool IsSQLite => string.Equals(Provider, "SQLite", StringComparison.OrdinalIgnoreCase);
        public bool IsSqlServer => string.Equals(Provider, "SqlServer", StringComparison.OrdinalIgnoreCase);

        public TimeSpan GetSessionInactivityTimeout()
        {
            return TimeSpan.FromMinutes(SessionInactivityTimeoutMinutes);
        }
    }

    public static class DatabaseConnectionSettingsService
    {
        private const string FileName = "database-settings.json";

        public static DatabaseConnectionSettings LoadOrCreateDefault(string appDataPath, LoggerService? logger = null)
        {
            Directory.CreateDirectory(appDataPath);

            var path = GetSettingsFilePath(appDataPath);
            if (!File.Exists(path))
            {
                var defaults = new DatabaseConnectionSettings();
                Save(appDataPath, defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<DatabaseConnectionSettings>(json) ?? new DatabaseConnectionSettings();

                if (!string.IsNullOrWhiteSpace(settings.SqlServerPassword) && DatabaseEncryptionService.IsEncrypted(settings.SqlServerPassword))
                {
                    settings.SqlServerPassword = DatabaseEncryptionService.DecryptPassword(settings.SqlServerPassword);
                }

                return Normalizar(settings);
            }
            catch (Exception ex)
            {
                var backupPath = Path.Combine(appDataPath, $"database-settings.invalid.{DateTime.Now:yyyyMMddHHmmss}.json");
                File.Copy(path, backupPath, overwrite: true);
                (logger ?? new LoggerService()).LogError($"Configuracao de banco invalida. Arquivo original preservado em '{backupPath}'.", ex);

                var defaults = new DatabaseConnectionSettings();
                Save(appDataPath, defaults);
                return defaults;
            }
        }

        public static void Save(string appDataPath, DatabaseConnectionSettings settings)
        {
            Directory.CreateDirectory(appDataPath);
            var path = GetSettingsFilePath(appDataPath);

            var settingsToSave = Normalizar(settings);

            if (!string.IsNullOrWhiteSpace(settingsToSave.SqlServerPassword))
            {
                settingsToSave.SqlServerPassword = DatabaseEncryptionService.EncryptPassword(settingsToSave.SqlServerPassword);
            }

            var json = JsonSerializer.Serialize(settingsToSave, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(path, json);
        }

        public static string GetSettingsFilePath(string appDataPath)
        {
            return Path.Combine(appDataPath, FileName);
        }

        private static DatabaseConnectionSettings Normalizar(DatabaseConnectionSettings settings)
        {
            settings.Provider = string.IsNullOrWhiteSpace(settings.Provider) ? "SQLite" : settings.Provider.Trim();
            settings.SQLitePath = string.IsNullOrWhiteSpace(settings.SQLitePath) ? "primoauto.db" : settings.SQLitePath.Trim();
            settings.SqlServerHost = string.IsNullOrWhiteSpace(settings.SqlServerHost) ? ".\\SQLEXPRESS" : settings.SqlServerHost.Trim();
            settings.SqlServerDatabase = string.IsNullOrWhiteSpace(settings.SqlServerDatabase) ? "PrimoAutoEletrica" : settings.SqlServerDatabase.Trim();
            settings.SqlUser = settings.SqlUser?.Trim() ?? string.Empty;
            settings.SqlPasswordProtected = settings.SqlPasswordProtected?.Trim() ?? string.Empty;
            settings.CommandTimeoutSeconds = Math.Clamp(
                settings.CommandTimeoutSeconds <= 0 ? DatabaseConnectionSettings.DefaultCommandTimeoutSeconds : settings.CommandTimeoutSeconds,
                DatabaseConnectionSettings.MinimumCommandTimeoutSeconds,
                DatabaseConnectionSettings.MaximumCommandTimeoutSeconds);
            settings.SessionInactivityTimeoutMinutes = Math.Clamp(
                settings.SessionInactivityTimeoutMinutes <= 0 ? DatabaseConnectionSettings.DefaultSessionInactivityTimeoutMinutes : settings.SessionInactivityTimeoutMinutes,
                DatabaseConnectionSettings.MinimumSessionInactivityTimeoutMinutes,
                DatabaseConnectionSettings.MaximumSessionInactivityTimeoutMinutes);
            return settings;
        }
    }
}
