using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Servico para gerenciar temas claro/escuro da aplicacao.
    /// </summary>
    public sealed class ThemeService
    {
        private const string ThemeSettingsFile = "theme_settings.json";
        private static readonly string ThemeSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PrimoAutoEletrica",
            ThemeSettingsFile);

        private AppTheme _currentTheme = AppTheme.Light;

        public AppTheme CurrentTheme => _currentTheme;

        public event EventHandler? ThemeChanged;

        public ThemeService()
        {
            LoadSavedTheme();
        }

        public void ApplyTheme(AppTheme theme)
        {
            var app = Application.Current;
            if (app == null)
            {
                _currentTheme = theme;
                return;
            }

            var resources = app.Resources;
            if (resources == null)
            {
                _currentTheme = theme;
                return;
            }

            var themeFile = GetThemeFile(theme);
            var colorsDict = resources.MergedDictionaries
                .FirstOrDefault(dictionary => dictionary.Source?.OriginalString.Contains("Themes/Colors.", StringComparison.OrdinalIgnoreCase) == true);

            if (_currentTheme == theme &&
                string.Equals(colorsDict?.Source?.OriginalString, themeFile, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _currentTheme = theme;

            if (colorsDict != null)
            {
                resources.MergedDictionaries.Remove(colorsDict);
            }

            try
            {
                resources.MergedDictionaries.Insert(0, new ResourceDictionary
                {
                    Source = new Uri(themeFile, UriKind.Relative)
                });
            }
            catch
            {
                var fallbackThemeFile = GetThemeFile(AppTheme.Light);
                _currentTheme = AppTheme.Light;

                resources.MergedDictionaries.Insert(0, new ResourceDictionary
                {
                    Source = new Uri(fallbackThemeFile, UriKind.Relative)
                });
            }

            SaveTheme(_currentTheme);
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleTheme()
        {
            ApplyTheme(_currentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
        }

        public AppTheme GetCurrentTheme()
        {
            return _currentTheme;
        }

        public void LoadSavedTheme()
        {
            try
            {
                if (!File.Exists(ThemeSettingsPath))
                {
                    _currentTheme = AppTheme.Light;
                    return;
                }

                var json = File.ReadAllText(ThemeSettingsPath);
                _currentTheme = json.Contains("\"Dark\"", StringComparison.OrdinalIgnoreCase) || json.Contains("2", StringComparison.Ordinal)
                    ? AppTheme.Dark
                    : AppTheme.Light;
            }
            catch
            {
                _currentTheme = AppTheme.Light;
            }
        }

        public void SaveTheme(AppTheme theme)
        {
            try
            {
                var directory = Path.GetDirectoryName(ThemeSettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = $"{{\"Theme\":{(int)theme}}}";
                File.WriteAllText(ThemeSettingsPath, json);
            }
            catch
            {
                // Silencia erros de persistencia do tema para nao bloquear a UI.
            }
        }

        private static string GetThemeFile(AppTheme theme)
        {
            return theme == AppTheme.Light
                ? "Themes/Colors.Light.xaml"
                : "Themes/Colors.Dark.xaml";
        }
    }

    public enum AppTheme
    {
        Light = 1,
        Dark = 2
    }
}
