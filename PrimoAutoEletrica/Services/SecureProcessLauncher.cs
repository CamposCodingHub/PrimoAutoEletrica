using System;
using System.Diagnostics;
using System.IO;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Hardened Process.Start for URLs and files under authorized roots (FULL ASSURANCE-12).
    /// </summary>
    public static class SecureProcessLauncher
    {
        public static void OpenUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                throw new ArgumentException("URI vazia.", nameof(uri));
            }

            var trimmed = uri.Trim();
            if (!(trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                  || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                  || trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)))
            {
                throw new UnauthorizedAccessException("Somente URIs http/https/mailto sao permitidas via OpenUri.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = trimmed,
                UseShellExecute = true
            });
        }

        public static void OpenFileOrDirectory(string path, params string[] additionalAllowedRoots)
        {
            var full = PathSecurityHelper.NormalizeFullPath(path);
            var roots = PathSecurityHelper.GetDefaultOpenFileRoots();
            if (additionalAllowedRoots is { Length: > 0 })
            {
                var merged = new string[roots.Length + additionalAllowedRoots.Length];
                roots.CopyTo(merged, 0);
                additionalAllowedRoots.CopyTo(merged, roots.Length);
                roots = merged;
            }

            PathSecurityHelper.RequireUnderAnyRoot(full, roots);

            if (!File.Exists(full) && !Directory.Exists(full))
            {
                throw new FileNotFoundException("Arquivo ou pasta nao encontrado(a).", full);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = full,
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Opens WhatsApp wa.me links (phone already sanitized by callers).
        /// </summary>
        public static void OpenWhatsAppLink(string waMeUrl)
        {
            if (string.IsNullOrWhiteSpace(waMeUrl))
            {
                throw new UnauthorizedAccessException("Link WhatsApp invalido.");
            }

            var trimmed = waMeUrl.Trim();
            if (!trimmed.StartsWith("https://wa.me/", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("http://wa.me/", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Link WhatsApp invalido.");
            }

            OpenUri(trimmed);
        }
    }
}
