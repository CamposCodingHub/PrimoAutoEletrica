using System;
using System.IO;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Path confinement helpers for backup/restore and file open (FULL ASSURANCE-12).
    /// </summary>
    public static class PathSecurityHelper
    {
        public static string NormalizeFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Caminho vazio.", nameof(path));
            }

            return Path.GetFullPath(path.Trim());
        }

        public static bool IsUnderRoot(string path, string root)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root))
            {
                return false;
            }

            var full = NormalizeFullPath(path);
            var rootFull = NormalizeFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                           + Path.DirectorySeparatorChar;
            return full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                       rootFull.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        public static string RequireUnderAnyRoot(string path, params string[] allowedRoots)
        {
            var full = NormalizeFullPath(path);
            foreach (var root in allowedRoots)
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                if (IsUnderRoot(full, root))
                {
                    return full;
                }
            }

            throw new UnauthorizedAccessException(
                "Caminho fora dos diretorios autorizados do PRIMOX. Operacao bloqueada por politica de seguranca.");
        }

        public static string[] GetDefaultRestoreRoots()
        {
            var appData = App.RuntimeAppDataPath;
            var localDefault = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica");
            return new[]
            {
                Path.Combine(appData, "Backups"),
                appData,
                Path.Combine(localDefault, "Backups"),
                // PackagingE2E / smoke isolated data roots
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PRIMOX-Workshop-DataTest-C08"),
            };
        }

        public static string[] GetDefaultOpenFileRoots()
        {
            var appData = App.RuntimeAppDataPath;
            return new[]
            {
                Path.Combine(appData, "Media"),
                Path.Combine(appData, "Exports"),
                Path.Combine(appData, "Imports"),
                Path.Combine(appData, "Logs"),
                Path.Combine(appData, "Backups"),
                appData,
                Path.GetTempPath()
            };
        }
    }
}
