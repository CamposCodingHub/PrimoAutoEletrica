using System;
using System.IO;
using System.Windows;

namespace PrimoAutoEletrica.Services
{
    public sealed class DisplayDensityService
    {
        private const string DensitySettingsFile = "density_settings.json";
        private static string DensitySettingsPath => Path.Combine(App.RuntimeAppDataPath, DensitySettingsFile);

        private DisplayDensity _currentDensity = DisplayDensity.Comfortable;

        public DisplayDensity CurrentDensity => _currentDensity;

        public event EventHandler? DensityChanged;

        public DisplayDensityService()
        {
            LoadSavedDensity();
        }

        public void ApplyDensity(DisplayDensity density)
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
            {
                _currentDensity = density;
                return;
            }

            _currentDensity = density;
            var compact = density == DisplayDensity.Compact;

            resources["DisplayDensityName"] = compact ? "Compact" : "Comfortable";
            resources["PageOuterMargin"] = compact ? new Thickness(14) : new Thickness(20);
            resources["PageSectionMargin"] = compact ? new Thickness(0, 0, 0, 14) : new Thickness(0, 0, 0, 20);
            resources["PageCardPadding"] = compact ? new Thickness(14) : new Thickness(22);
            resources["PageToolbarPadding"] = compact ? new Thickness(12) : new Thickness(16);
            resources["PageInputPadding"] = compact ? new Thickness(10, 0, 10, 0) : new Thickness(12, 0, 12, 0);
            resources["DensityControlHeight"] = compact ? 34d : 40d;
            resources["DensityCompactControlHeight"] = compact ? 30d : 34d;
            resources["DensityTableRowHeight"] = compact ? 30d : 38d;
            resources["DensityTableCompactRowHeight"] = compact ? 26d : 30d;
            // Alinhado aos tokens PRIMOX (cards 10–12); não restaurar raio legado 18.
            resources["DensityCardRadius"] = compact ? 10d : 12d;
            resources["DensityCompactCardRadius"] = compact ? 10d : 12d;

            SaveDensity(density);
            DensityChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleDensity()
        {
            ApplyDensity(_currentDensity == DisplayDensity.Compact
                ? DisplayDensity.Comfortable
                : DisplayDensity.Compact);
        }

        public DisplayDensity GetCurrentDensity()
        {
            return _currentDensity;
        }

        private void LoadSavedDensity()
        {
            try
            {
                if (!File.Exists(DensitySettingsPath))
                {
                    _currentDensity = DisplayDensity.Comfortable;
                    return;
                }

                var json = File.ReadAllText(DensitySettingsPath);
                _currentDensity = json.Contains("\"Compact\"", StringComparison.OrdinalIgnoreCase) || json.Contains("2", StringComparison.Ordinal)
                    ? DisplayDensity.Compact
                    : DisplayDensity.Comfortable;
            }
            catch
            {
                _currentDensity = DisplayDensity.Comfortable;
            }
        }

        private static void SaveDensity(DisplayDensity density)
        {
            try
            {
                var directory = Path.GetDirectoryName(DensitySettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(DensitySettingsPath, $"{{\"Density\":{(int)density}}}");
            }
            catch
            {
                // A densidade visual nunca deve bloquear o uso do sistema.
            }
        }
    }

    public enum DisplayDensity
    {
        Comfortable = 1,
        Compact = 2
    }
}
