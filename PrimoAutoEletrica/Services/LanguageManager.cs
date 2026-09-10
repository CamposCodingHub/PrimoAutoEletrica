using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Facade legada sobre <see cref="LocalizationService"/>. Preferir LocalizationService diretamente.
    /// </summary>
    public class LanguageManager
    {
        private static LanguageManager? _instance;

        public static LanguageManager Instance
        {
            get
            {
                _instance ??= new LanguageManager();
                return _instance;
            }
        }

        public event EventHandler? LanguageChanged;

        public LanguageManager()
        {
            LocalizationService.Instance.CultureChanged += (_, _) => LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        public CultureInfo CurrentCulture
        {
            get => LocalizationService.Instance.CurrentCulture;
            set
            {
                if (value == null)
                {
                    return;
                }

                LocalizationService.Instance.CurrentCulture = value;
            }
        }

        public string CurrentLanguageName => CurrentCulture.DisplayName;

        public string CurrentLanguageCode => CurrentCulture.Name;

        public List<LanguageInfo> SupportedLanguages => new()
        {
            new LanguageInfo("Português", "pt-BR", CultureInfo.GetCultureInfo("pt-BR")),
            new LanguageInfo("English", "en-US", CultureInfo.GetCultureInfo("en-US")),
            new LanguageInfo("Español", "es-ES", CultureInfo.GetCultureInfo("es-ES"))
        };

        public void ChangeLanguage(string languageCode)
        {
            LocalizationService.Instance.SetLanguage(languageCode);
        }

        public LanguageInfo? GetLanguageInfo(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            return SupportedLanguages.FirstOrDefault(l =>
                l.Code.Equals(code, StringComparison.OrdinalIgnoreCase) ||
                l.Culture.TwoLetterISOLanguageName.Equals(code, StringComparison.OrdinalIgnoreCase) ||
                l.Culture.Name.Equals(code, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class LanguageInfo
    {
        public LanguageInfo(string displayName, string code, CultureInfo culture)
        {
            DisplayName = displayName;
            Code = code;
            Culture = culture;
        }

        public string DisplayName { get; set; }
        public string Code { get; set; }
        public CultureInfo Culture { get; set; }

        public override string ToString() => DisplayName;
    }
}
