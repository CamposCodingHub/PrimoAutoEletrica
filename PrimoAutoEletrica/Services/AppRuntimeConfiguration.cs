using System;
using System.IO;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public sealed class AppRuntimeConfiguration
    {
        public string ModeName { get; init; } = "normal";
        public string AppDataPath { get; init; } = string.Empty;
        public string LogDirectory { get; init; } = string.Empty;
        public string BackupDirectory { get; init; } = string.Empty;
        public string SmokeFilter { get; init; } = string.Empty;
        public bool IsSmokeTestMode { get; init; }
        public bool IsWorkflowTestMode { get; init; }
        public bool IsSmokeVisible { get; init; }
        public string CatalogoImportPath { get; init; } = string.Empty;
        public string CatalogoImportMarca { get; init; } = string.Empty;
        public bool CatalogoPurgeMarca { get; init; }

        public bool IsAutomatedTestMode => IsSmokeTestMode || IsWorkflowTestMode;
        public bool IsCatalogoImportMode => !string.IsNullOrWhiteSpace(CatalogoImportPath);

        public static AppRuntimeConfiguration FromArgs(string[]? args)
        {
            var arguments = args ?? Array.Empty<string>();
            var isWorkflowTest = arguments.Any(arg => string.Equals(arg, "--workflow-test", StringComparison.OrdinalIgnoreCase));
            var isSmokeTest = arguments.Any(arg => string.Equals(arg, "--smoke-test", StringComparison.OrdinalIgnoreCase));
            var isSmokeVisible = arguments.Any(arg => string.Equals(arg, "--smoke-visible", StringComparison.OrdinalIgnoreCase));
            var smokeFilter = arguments
                .FirstOrDefault(arg => arg.StartsWith("--smoke-filter=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--smoke-filter=".Length)
                ?.Trim()
                ?? string.Empty;
            var appDataOverride = arguments
                .FirstOrDefault(arg => arg.StartsWith("--app-data=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--app-data=".Length)
                ?.Trim();
            var catalogoImportPath = arguments
                .FirstOrDefault(arg => arg.StartsWith("--catalogo-import=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--catalogo-import=".Length)
                ?.Trim()
                ?? string.Empty;
            var catalogoImportMarca = arguments
                .FirstOrDefault(arg => arg.StartsWith("--catalogo-marca=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--catalogo-marca=".Length)
                ?.Trim()
                ?? string.Empty;
            var catalogoPurgeMarca = arguments.Any(arg =>
                string.Equals(arg, "--catalogo-purge-marca", StringComparison.OrdinalIgnoreCase));

            if (isWorkflowTest || isSmokeTest)
            {
                return CreateAutomatedRuntime(
                    isWorkflowTest ? "workflow-test" : "ui-smoke-test",
                    isSmokeTest,
                    isWorkflowTest,
                    smokeFilter,
                    appDataOverride,
                    isSmokeVisible);
            }

            var runtime = CreateDefault();
            if (string.IsNullOrWhiteSpace(catalogoImportPath))
            {
                return runtime;
            }

            return new AppRuntimeConfiguration
            {
                ModeName = "catalogo-import",
                AppDataPath = runtime.AppDataPath,
                LogDirectory = runtime.LogDirectory,
                BackupDirectory = runtime.BackupDirectory,
                CatalogoImportPath = catalogoImportPath,
                CatalogoImportMarca = catalogoImportMarca,
                CatalogoPurgeMarca = catalogoPurgeMarca
            };
        }

        private static string? _testHostAppDataPath;

        public static AppRuntimeConfiguration CreateDefault()
        {
            var isRunningInTest = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => {
                    var n = a.GetName().Name ?? string.Empty;
                    return n.Contains("xunit", StringComparison.OrdinalIgnoreCase) ||
                           n.Contains("testhost", StringComparison.OrdinalIgnoreCase);
                });

            if (isRunningInTest)
            {
                _testHostAppDataPath ??= Path.Combine(Path.GetTempPath(), "PrimoAuto_TestHost_" + Environment.ProcessId);
                Directory.CreateDirectory(_testHostAppDataPath);
                return new AppRuntimeConfiguration
                {
                    ModeName = "test-host-isolated",
                    AppDataPath = _testHostAppDataPath,
                    LogDirectory = Path.Combine(_testHostAppDataPath, "Logs"),
                    BackupDirectory = Path.Combine(_testHostAppDataPath, "Backups")
                };
            }

            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica");

            return new AppRuntimeConfiguration
            {
                ModeName = "normal",
                AppDataPath = root,
                // Logs em AppData (gravavel) — nao em Program Files / BaseDirectory.
                LogDirectory = Path.Combine(root, "Logs"),
                BackupDirectory = Path.Combine(root, "Backups")
            };
        }

        private static AppRuntimeConfiguration CreateAutomatedRuntime(
            string modeName,
            bool isSmokeTest,
            bool isWorkflowTest,
            string smokeFilter,
            string? appDataOverride = null,
            bool isSmokeVisible = false)
        {
            string defaultBase;
            var productionRoot = Path.GetFullPath(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimoAutoEletrica"));
            var baseDir = Path.GetFullPath(AppContext.BaseDirectory);
            if (string.Equals(baseDir, productionRoot, StringComparison.OrdinalIgnoreCase) ||
                baseDir.StartsWith(productionRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                defaultBase = Path.Combine(Path.GetTempPath(), "PrimoAuto_Automated");
            }
            else
            {
                defaultBase = AppContext.BaseDirectory;
            }

            var automatedRoot = string.IsNullOrWhiteSpace(appDataOverride)
                ? Path.Combine(
                    defaultBase,
                    "AutomatedTests",
                    $"{modeName}-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}")
                : appDataOverride;

            Directory.CreateDirectory(automatedRoot);

            return new AppRuntimeConfiguration
            {
                ModeName = modeName,
                AppDataPath = automatedRoot,
                LogDirectory = Path.Combine(automatedRoot, "Logs"),
                BackupDirectory = Path.Combine(automatedRoot, "Backups"),
                SmokeFilter = smokeFilter,
                IsSmokeTestMode = isSmokeTest,
                IsWorkflowTestMode = isWorkflowTest,
                IsSmokeVisible = isSmokeVisible
            };
        }
    }
}
