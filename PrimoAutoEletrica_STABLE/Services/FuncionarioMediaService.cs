using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace PrimoAutoEletrica.Services
{
    public static class FuncionarioMediaService
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        public static string SupportedImageFilter => "Imagens|*.jpg;*.jpeg;*.png;*.webp";

        public static string GetManagedMediaDirectory()
        {
            var directory = Path.Combine(App.RuntimeAppDataPath, "Media", "Funcionarios");
            Directory.CreateDirectory(directory);
            return directory;
        }

        public static bool IsSupportedImageFile(string? path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && SupportedExtensions.Contains(Path.GetExtension(path));
        }

        public static string PersistSelectedImage(string sourcePath, int funcionarioId, string nomeFuncionario)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                throw new FileNotFoundException("A imagem selecionada para o funcionario nao foi encontrada.", sourcePath);
            }

            if (!IsSupportedImageFile(sourcePath))
            {
                throw new InvalidOperationException("Selecione uma imagem valida (.jpg, .jpeg, .png ou .webp).");
            }

            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            var destinationDirectory = GetManagedMediaDirectory();
            var safeName = SanitizeFileSegment(nomeFuncionario);
            var destinationPath = Path.Combine(
                destinationDirectory,
                $"{DateTime.Now:yyyyMMddHHmmss}_{safeName}_{funcionarioId}{extension}");

            File.Copy(sourcePath, destinationPath, overwrite: true);
            return destinationPath;
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
                // Nao interrompe o fluxo se o arquivo ja foi removido.
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

        private static string SanitizeFileSegment(string? value)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "funcionario" : value.Trim();
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(invalidChar, '_');
            }

            return text.Replace(' ', '_');
        }
    }
}
