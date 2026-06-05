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

        public bool IsAutomatedTestMode => IsSmokeTestMode || IsWorkflowTestMode;

        public static AppRuntimeConfiguration FromArgs(string[]? args)
        {
            var arguments = args ?? Array.Empty<string>();
            var isWorkflowTest = arguments.Any(arg => string.Equals(arg, "--workflow-test", StringComparison.OrdinalIgnoreCase));
            var isSmokeTest = arguments.Any(arg => string.Equals(arg, "--smoke-test", StringComparison.OrdinalIgnoreCase));
            var smokeFilter = arguments
                .FirstOrDefault(arg => arg.StartsWith("--smoke-filter=", StringComparison.OrdinalIgnoreCase))
                ?.Substring("--smoke-filter=".Length)
                ?.Trim()
                ?? string.Empty;

            return isWorkflowTest || isSmokeTest
                ? CreateAutomatedRuntime(isWorkflowTest ? "workflow-test" : "ui-smoke-test", isSmokeTest, isWorkflowTest, smokeFilter)
                : CreateDefault();
        }

        public static AppRuntimeConfiguration CreateDefault()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica");

            return new AppRuntimeConfiguration
            {
                ModeName = "normal",
                AppDataPath = root,
                LogDirectory = Path.Combine(AppContext.BaseDirectory, "Logs"),
                BackupDirectory = Path.Combine(root, "Backups")
            };
        }

        private static AppRuntimeConfiguration CreateAutomatedRuntime(string modeName, bool isSmokeTest, bool isWorkflowTest, string smokeFilter)
        {
            var automatedRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PrimoAutoEletrica",
                "AutomatedTests",
                $"{modeName}-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}");

            return new AppRuntimeConfiguration
            {
                ModeName = modeName,
                AppDataPath = automatedRoot,
                LogDirectory = Path.Combine(AppContext.BaseDirectory, "Logs"),
                BackupDirectory = Path.Combine(automatedRoot, "Backups"),
                SmokeFilter = smokeFilter,
                IsSmokeTestMode = isSmokeTest,
                IsWorkflowTestMode = isWorkflowTest
            };
        }
    }
}
