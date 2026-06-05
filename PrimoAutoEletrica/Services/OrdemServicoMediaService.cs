using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace PrimoAutoEletrica.Services
{
    public static class OrdemServicoMediaService
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        public static string SupportedImageFilter => "Imagens|*.jpg;*.jpeg;*.png;*.webp";

        public static bool IsSupportedImageFile(string? path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && SupportedExtensions.Contains(Path.GetExtension(path));
        }

        public static string GetManagedMediaDirectory()
        {
            var directory = Path.Combine(App.RuntimeAppDataPath, "Media", "OrdensServico");
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static string PersistSelectedImage(string sourcePath, Guid ordemId, string numeroOrdem, string categoria)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                throw new FileNotFoundException("A imagem selecionada para a ordem de servico nao foi encontrada.", sourcePath);
            }

            if (!IsSupportedImageFile(sourcePath))
            {
                throw new InvalidOperationException("Selecione uma imagem valida (.jpg, .jpeg, .png ou .webp).");
            }

            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            var destinationDirectory = Path.Combine(GetManagedMediaDirectory(), SanitizeFileSegment(categoria));
            Directory.CreateDirectory(destinationDirectory);

            var destinationPath = Path.Combine(
                destinationDirectory,
                $"{DateTime.Now:yyyyMMddHHmmss}_{SanitizeFileSegment(numeroOrdem)}_{ordemId:N}{extension}");

            File.Copy(sourcePath, destinationPath, overwrite: true);
            return destinationPath;
        }

        public static string SerializePaths(IEnumerable<string> paths)
        {
            return string.Join("|", (paths ?? Enumerable.Empty<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim()));
        }

        public static List<string> DeserializePaths(string? serialized)
        {
            return (serialized ?? string.Empty)
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();
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
                // Nao bloqueia o fluxo se o arquivo ja foi removido.
            }
        }

        private static string SanitizeFileSegment(string? value)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "ordem_servico" : value.Trim();
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(invalidChar, '_');
            }

            return text.Replace(' ', '_');
        }
    }
}
