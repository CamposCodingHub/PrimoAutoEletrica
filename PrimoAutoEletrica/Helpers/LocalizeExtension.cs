using System;
using System.Windows.Markup;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// Extensão de Markup para localização em XAML
    /// Uso: Text="{local:Localize Dashboard}"
    /// </summary>
    public class LocalizeExtension : MarkupExtension
    {
        private string _key;

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public LocalizeExtension()
        {
            _key = string.Empty;
        }

        public LocalizeExtension(string key)
        {
            _key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(_key))
            {
                return string.Empty;
            }

            return LocalizationService.Instance.GetString(_key);
        }
    }
}
