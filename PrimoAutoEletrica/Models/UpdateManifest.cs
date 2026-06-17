using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models
{
    public class UpdateManifest
    {
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public bool UpdateAvailable { get; set; }
        public bool UpdateMandatory { get; set; }
        public DateTime ReleaseDate { get; set; }
        public UpdatePackageInfo Package { get; set; } = new();
        public List<UpdateChange> Changes { get; set; } = new();
        public UpdateCompatibility Compatibility { get; set; } = new();
    }

    public class UpdatePackageInfo
    {
        public string Url { get; set; } = string.Empty;
        public string LocalPath { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public long Size { get; set; }
        public string MinVersion { get; set; } = string.Empty;
    }

    public class UpdateChange
    {
        public string Type { get; set; } = string.Empty; // feature, fix, improvement, breaking
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateCompatibility
    {
        public string MinWindowsVersion { get; set; } = "6.1"; // Windows 7 SP1
        public long RequiredDiskSpace { get; set; } = 500000000; // 500 MB
        public long RequiredRAM { get; set; } = 2147483648; // 2 GB
    }

    public class UpdateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BackupPath { get; set; }
        public DateTime UpdateTime { get; set; }
        public List<string> LogEntries { get; set; } = new();
    }

    public enum UpdateStatus
    {
        Idle,
        Checking,
        Downloading,
        Validating,
        CreatingBackup,
        ApplyingUpdate,
        ValidatingUpdate,
        Completed,
        Failed,
        RollingBack
    }
}
