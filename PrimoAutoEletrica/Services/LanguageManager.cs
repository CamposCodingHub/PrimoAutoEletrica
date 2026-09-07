using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Gerenciador de idiomas da aplicação
    /// Permite trocar entre idiomas suportados e notifica observadores
    /// </summary>
    public class LanguageManager
    {
        private static LanguageManager? _instance;
        private CultureInfo _currentCulture;

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
            _currentCulture = CultureInfo.CurrentCulture;
        }

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    LocalizationService.Instance.CurrentCulture = value;
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string CurrentLanguageName
        {
            get => _currentCulture.DisplayName;
        }

        public string CurrentLanguageCode
        {
            get => _currentCulture.TwoLetterISOLanguageName.ToUpper();
        }

        /// <summary>
        /// Idiomas suportados pela aplicação
        /// </summary>
        public List<LanguageInfo> SupportedLanguages => new()
        {
            new LanguageInfo("Português", "pt", new CultureInfo("pt-BR")),
            new LanguageInfo("English", "en", new CultureInfo("en-US")),
            new LanguageInfo("Español", "es", new CultureInfo("es-ES"))
        };

        /// <summary>
        /// Muda o idioma da aplicação
        /// </summary>
        public void ChangeLanguage(string languageCode)
        {
            var language = SupportedLanguages.FirstOrDefault(l => l.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
            if (language != null)
            {
                CurrentCulture = language.Culture;
            }
        }

        /// <summary>
        /// Obtém informações sobre um idioma
        /// </summary>
        public LanguageInfo? GetLanguageInfo(string code)
        {
            return SupportedLanguages.FirstOrDefault(l => l.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Informações sobre um idioma suportado
    /// </summary>
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
