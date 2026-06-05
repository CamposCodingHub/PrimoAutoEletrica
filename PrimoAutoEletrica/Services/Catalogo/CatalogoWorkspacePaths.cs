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
