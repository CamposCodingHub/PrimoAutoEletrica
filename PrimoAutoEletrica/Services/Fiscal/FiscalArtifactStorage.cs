using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PrimoAutoEletrica.Services.Fiscal
{
    /// <summary>
    /// Armazenamento path-safe de XML/PDF fiscais sob AppData (anti path-traversal).
    /// </summary>
    public sealed class FiscalArtifactStorage
    {
        private readonly string _root;

        public FiscalArtifactStorage(string appDataPath)
        {
            if (string.IsNullOrWhiteSpace(appDataPath))
            {
                throw new ArgumentException("AppData path obrigatorio.", nameof(appDataPath));
            }

            _root = Path.GetFullPath(Path.Combine(appDataPath, "FiscalArtifacts"));
            Directory.CreateDirectory(_root);
        }

        public string RootPath => _root;

        public string SaveXml(Guid empresaId, Guid operationId, string fileName, string xmlContent)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(xmlContent);
            var path = ResolveSafePath(empresaId, operationId, EnsureExtension(fileName, ".xml"));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, xmlContent, Encoding.UTF8);
            return path;
        }

        public string SaveBytes(Guid empresaId, Guid operationId, string fileName, byte[] content)
        {
            ArgumentNullException.ThrowIfNull(content);
            var path = ResolveSafePath(empresaId, operationId, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, content);
            return path;
        }

        public string? TryReadText(string absolutePath)
        {
            if (!IsUnderRoot(absolutePath) || !File.Exists(absolutePath))
            {
                return null;
            }

            return File.ReadAllText(absolutePath, Encoding.UTF8);
        }

        public byte[]? TryReadBytes(string absolutePath)
        {
            if (!IsUnderRoot(absolutePath) || !File.Exists(absolutePath))
            {
                return null;
            }

            return File.ReadAllBytes(absolutePath);
        }

        public static string Sha256Hex(byte[] content)
        {
            var hash = SHA256.HashData(content);
            return Convert.ToHexString(hash);
        }

        private string ResolveSafePath(Guid empresaId, Guid operationId, string fileName)
        {
            var safeName = SanitizeFileName(fileName);
            var candidate = Path.GetFullPath(Path.Combine(
                _root,
                empresaId == Guid.Empty ? "_default" : empresaId.ToString("N"),
                operationId.ToString("N"),
                safeName));

            if (!IsUnderRoot(candidate))
            {
                throw new InvalidOperationException("Caminho de artefato fiscal fora do root permitido (path traversal).");
            }

            return candidate;
        }

        private bool IsUnderRoot(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return false;
            }

            var full = Path.GetFullPath(absolutePath);
            var root = _root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       + Path.DirectorySeparatorChar;
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(full, _root, StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName.Contains("..", StringComparison.Ordinal) ||
                fileName.Contains('/') ||
                fileName.Contains('\\') ||
                Path.IsPathRooted(fileName))
            {
                throw new InvalidOperationException("Nome de artefato fiscal invalido (path traversal).");
            }

            var name = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Nome de artefato fiscal vazio.");
            }

            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            if (name is "." or "..")
            {
                throw new InvalidOperationException("Nome de artefato fiscal invalido.");
            }

            return name;
        }

        private static string EnsureExtension(string fileName, string extension)
        {
            var name = SanitizeFileName(fileName);
            return name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                ? name
                : Path.ChangeExtension(name, extension);
        }
    }
}
