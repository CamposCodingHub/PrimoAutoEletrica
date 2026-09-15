using System;
using System.IO;

namespace PrimoAutoEletrica.Services.Catalogo
{
    internal static class CatalogoWorkspacePaths
    {
        public static string GetImportReportDirectory()
        {
            var projectRoot = FindProjectRoot();
            if (!string.IsNullOrWhiteSpace(projectRoot))
            {
                return EnsureDirectory(Path.Combine(projectRoot, "Docs", "ImportacoesCatalogo"));
            }

            return EnsureDirectory(Path.Combine(global::PrimoAutoEletrica.App.RuntimeAppDataPath, "Docs", "ImportacoesCatalogo"));
        }

        public static string GetExportDirectory()
        {
            var projectRoot = FindProjectRoot();
            if (!string.IsNullOrWhiteSpace(projectRoot))
            {
                return EnsureDirectory(Path.Combine(projectRoot, "Docs", "ExportacoesCatalogo"));
            }

            return EnsureDirectory(Path.Combine(global::PrimoAutoEletrica.App.RuntimeAppDataPath, "Exports", "Catalogo"));
        }

        public static string GetImagesDirectory(string? marca = null, string? importacaoId = null)
        {
            var root = EnsureDirectory(Path.Combine(global::PrimoAutoEletrica.App.RuntimeAppDataPath, "Catalogo", "Imagens"));
            if (!string.IsNullOrWhiteSpace(marca))
            {
                root = EnsureDirectory(Path.Combine(root, SanitizeFolder(marca)));
            }

            if (!string.IsNullOrWhiteSpace(importacaoId))
            {
                root = EnsureDirectory(Path.Combine(root, SanitizeFolder(importacaoId)));
            }

            return root;
        }

        private static string SanitizeFolder(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return string.IsNullOrWhiteSpace(value) ? "geral" : value.Trim();
        }

        private static string FindProjectRoot()
        {
            try
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "PrimoAutoEletrica.csproj")))
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
