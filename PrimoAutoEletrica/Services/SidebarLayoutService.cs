using System;
using System.IO;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Preferência de largura da sidebar PRIMOX (expandida 240 / compacta 68).
    /// Persistência local sem dados sensíveis.
    /// </summary>
    public sealed class SidebarLayoutService
    {
        public const double ExpandedWidth = 240d;
        public const double CollapsedWidth = 68d;

        private const string SettingsFile = "sidebar_settings.json";
        private static string SettingsPath => Path.Combine(App.RuntimeAppDataPath, SettingsFile);

        private bool _isExpanded = true;

        public bool IsExpanded => _isExpanded;

        public double CurrentWidth => _isExpanded ? ExpandedWidth : CollapsedWidth;

        public event EventHandler? LayoutChanged;

        public SidebarLayoutService()
        {
            LoadSavedLayout();
        }

        public void ApplyExpanded(bool expanded)
        {
            if (_isExpanded == expanded)
            {
                return;
            }

            _isExpanded = expanded;
            SaveLayout(_isExpanded);
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Toggle()
        {
            ApplyExpanded(!_isExpanded);
        }

        public void LoadSavedLayout()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    _isExpanded = true;
                    return;
                }

                var json = File.ReadAllText(SettingsPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    _isExpanded = true;
                    return;
                }

                // Aceita {"Expanded":true|false} ou literais legados.
                if (json.Contains("\"Expanded\":false", StringComparison.OrdinalIgnoreCase)
                    || json.Contains("\"Collapsed\":true", StringComparison.OrdinalIgnoreCase)
                    || json.Contains("\"IsExpanded\":false", StringComparison.OrdinalIgnoreCase))
                {
                    _isExpanded = false;
                    return;
                }

                if (json.Contains("\"Expanded\":true", StringComparison.OrdinalIgnoreCase)
                    || json.Contains("\"IsExpanded\":true", StringComparison.OrdinalIgnoreCase))
                {
                    _isExpanded = true;
                    return;
                }

                _isExpanded = true;
            }
            catch
            {
                _isExpanded = true;
            }
        }

        private static void SaveLayout(bool expanded)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SettingsPath, $"{{\"Expanded\":{(expanded ? "true" : "false")}}}");
            }
            catch
            {
                // Preferência visual nunca deve bloquear o shell.
            }
        }
    }
}
