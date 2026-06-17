using System;

namespace PrimoAutoEletrica.Models
{
    public class BackupSettings
    {
        public AutoBackupSettings AutoBackup { get; set; } = new();
        public ExternalBackupSettings ExternalBackup { get; set; } = new();
        public CompressionSettings Compression { get; set; } = new();
        public RetentionPolicy Retention { get; set; } = new();
    }

    public class AutoBackupSettings
    {
        public bool Enabled { get; set; } = true;
        public string Frequency { get; set; } = "daily"; // daily, weekly, monthly
        public string Time { get; set; } = "02:00";
        public bool BeforeUpdate { get; set; } = true;
        public bool BeforeMigration { get; set; } = true;
        public bool IncludeMedia { get; set; } = true;
        public bool IncludeLogs { get; set; } = false;
    }

    public class ExternalBackupSettings
    {
        public bool Enabled { get; set; } = false;
        public string Destination { get; set; } = string.Empty;
        public bool SyncWithCloud { get; set; } = false;
        public string CloudProvider { get; set; } = "none"; // none, onedrive, googledrive, dropbox
        public string CloudPath { get; set; } = string.Empty;
        public bool Encrypt { get; set; } = false;
        public string? EncryptionPassword { get; set; }
    }

    public class CompressionSettings
    {
        public bool Enabled { get; set; } = true;
        public string Format { get; set; } = "zip"; // zip, 7z
        public string Level { get; set; } = "normal"; // none, fast, normal, maximum
        public string? Password { get; set; }
    }

    public class RetentionPolicy
    {
        public int DailyBackups { get; set; } = 30;
        public int WeeklyBackups { get; set; } = 12;
        public int MonthlyBackups { get; set; } = 6;
        public int PreUpdateBackups { get; set; } = 10;
        public int ManualBackups { get; set; } = -1; // -1 = keep all
        public bool AutoClean { get; set; } = true;
        public string CleanTime { get; set; } = "03:00";
    }

    public class BackupInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // auto, manual, preupdate, premigration
        public DateTime CreatedAt { get; set; }
        public long Size { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public bool Compressed { get; set; }
        public bool IncludesMedia { get; set; }
        public bool IncludesLogs { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class BackupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BackupPath { get; set; }
        public BackupInfo? BackupInfo { get; set; }
        public DateTime BackupTime { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio { get; set; }
    }
}
