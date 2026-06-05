using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace PrimoAutoEletrica.Services
{
    public static class ProdutoMediaService
    {
        private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private static readonly HashSet<string> SupportedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".pdf",
            ".txt",
            ".csv",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx"
        };

        public static string SupportedImageFilter => "Imagens|*.jpg;*.jpeg;*.png;*.webp";
        public static string SupportedAttachmentFilter => "Anexos do produto|*.jpg;*.jpeg;*.png;*.webp;*.pdf;*.txt;*.csv;*.doc;*.docx;*.xls;*.xlsx|Todos os arquivos suportados|*.jpg;*.jpeg;*.png;*.webp;*.pdf;*.txt;*.csv;*.doc;*.docx;*.xls;*.xlsx";

        public static string GetManagedMediaDirectory()
        {
            var directory = Path.Combine(App.RuntimeAppDataPath, "Media", "Produtos");
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static string GetManagedAttachmentDirectory(Guid produtoId)
        {
            var directory = Path.Combine(GetManagedMediaDirectory(), "Anexos", produtoId.ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static bool IsSupportedImageFile(string? path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && SupportedImageExtensions.Contains(Path.GetExtension(path));
        }

        public static bool IsSupportedAttachmentFile(string? path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && SupportedAttachmentExtensions.Contains(Path.GetExtension(path));
        }

        public static string PersistSelectedImage(string sourcePath, Guid produtoId, string nomeProduto)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                throw new FileNotFoundException("A imagem selecionada para o produto nao foi encontrada.", sourcePath);
            }

            if (!IsSupportedImageFile(sourcePath))
            {
                throw new InvalidOperationException("Selecione uma imagem valida (.jpg, .jpeg, .png ou .webp).");
            }

            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            var destinationDirectory = GetManagedMediaDirectory();
            var safeName = SanitizeFileSegment(nomeProduto);
            var destinationPath = Path.Combine(
                destinationDirectory,
                $"{DateTime.Now:yyyyMMddHHmmss}_{safeName}_{produtoId:N}{extension}");

            File.Copy(sourcePath, destinationPath, overwrite: true);
            return destinationPath;
        }

        public static string PersistSelectedAttachment(string sourcePath, Guid produtoId, string nomeProduto)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                throw new FileNotFoundException("O anexo selecionado para o produto nao foi encontrado.", sourcePath);
            }

            if (!IsSupportedAttachmentFile(sourcePath))
            {
                throw new InvalidOperationException("Selecione um anexo valido (.jpg, .jpeg, .png, .webp, .pdf, .txt, .csv, .doc, .docx, .xls ou .xlsx).");
            }

            var resolvedSource = Path.GetFullPath(sourcePath);
            if (IsManagedAttachmentPath(resolvedSource, produtoId))
            {
                return resolvedSource;
            }

            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            var destinationDirectory = GetManagedAttachmentDirectory(produtoId);
            var safeProductName = SanitizeFileSegment(nomeProduto);
            var safeOriginalName = SanitizeFileSegment(Path.GetFileNameWithoutExtension(sourcePath));
            var destinationPath = Path.Combine(
                destinationDirectory,
                $"{DateTime.Now:yyyyMMddHHmmssfff}_{safeProductName}_{safeOriginalName}{extension}");

            File.Copy(sourcePath, destinationPath, overwrite: true);
            return destinationPath;
        }

        public static string PersistSelectedAttachments(IEnumerable<string> sourcePaths, Guid produtoId, string nomeProduto)
        {
            var persistedPaths = new List<string>();
            foreach (var sourcePath in sourcePaths)
            {
                var resolved = ResolveExistingPath(sourcePath);
                if (string.IsNullOrWhiteSpace(resolved))
                {
                    continue;
                }

                persistedPaths.Add(PersistSelectedAttachment(resolved, produtoId, nomeProduto));
            }

            return SerializeAttachmentPaths(persistedPaths);
        }

        public static void DeleteManagedImageIfOwned(string? persistedPath)
        {
            var normalizedPath = ResolveExistingPath(persistedPath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return;
            }

            var managedDirectory = Path.GetFullPath(GetManagedMediaDirectory()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!normalizedPath.StartsWith(managedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                File.Delete(normalizedPath);
            }
            catch
            {
                // Nao bloqueia o fluxo da tela se o arquivo ja tiver sido removido externamente.
            }
        }

        public static void DeleteManagedAttachmentIfOwned(string? persistedPath)
        {
            var normalizedPath = ResolveExistingPath(persistedPath);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return;
            }

            var managedDirectory = Path.GetFullPath(Path.Combine(GetManagedMediaDirectory(), "Anexos")).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!normalizedPath.StartsWith(managedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                File.Delete(normalizedPath);
            }
            catch
            {
                // Nao bloqueia o fluxo da tela se o arquivo ja tiver sido removido externamente.
            }
        }

        public static void DeleteManagedAttachmentsNotIn(string? previousSerializedPaths, string? currentSerializedPaths)
        {
            var current = DeserializeAttachmentPaths(currentSerializedPaths)
                .Select(path => Path.GetFullPath(path))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var previous in DeserializeAttachmentPaths(previousSerializedPaths))
            {
                var resolved = ResolveExistingPath(previous);
                if (string.IsNullOrWhiteSpace(resolved) || current.Contains(Path.GetFullPath(resolved)))
                {
                    continue;
                }

                DeleteManagedAttachmentIfOwned(resolved);
            }
        }

        public static string ResolveExistingPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                var fullPath = Path.GetFullPath(path);
                return File.Exists(fullPath) ? fullPath : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static BitmapImage? TryCreatePreviewSource(string? path)
        {
            var resolvedPath = ResolveExistingPath(path);
            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                return null;
            }

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(resolvedPath, UriKind.Absolute);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }

        public static string SerializeAttachmentPaths(IEnumerable<string> paths)
        {
            return string.Join(
                Environment.NewLine,
                paths
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => path.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        public static List<string> DeserializeAttachmentPaths(string? serializedPaths)
        {
            if (string.IsNullOrWhiteSpace(serializedPaths))
            {
                return new List<string>();
            }

            return serializedPaths
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(path => path.Trim())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static bool IsManagedAttachmentPath(string path, Guid produtoId)
        {
            var attachmentDirectory = Path.GetFullPath(GetManagedAttachmentDirectory(produtoId)).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(attachmentDirectory, StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizeFileSegment(string? value)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "produto" : value.Trim();
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(invalidChar, '_');
            }

            return text.Replace(' ', '_');
        }
    }
}
