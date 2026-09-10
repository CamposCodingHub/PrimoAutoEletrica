using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Helpers
{
    /// <summary>
    /// Markup Extension com refresh em runtime via LocalizationHelper indexer.
    /// Uso: Text="{helpers:Localize Clients}"
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public class LocalizeExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public LocalizeExtension()
        {
        }

        public LocalizeExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                return string.Empty;
            }

            // Preferir instancia do App resources quando disponivel; fallback Instance.
            object source = LocalizationHelper.Instance;
            if (Application.Current?.TryFindResource("LocalizationHelper") is LocalizationHelper appHelper)
            {
                source = appHelper;
            }

            var binding = new Binding($"[{Key}]")
            {
                Source = source,
                Mode = BindingMode.OneWay
            };

            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target
                && target.TargetObject is DependencyObject
                && target.TargetProperty is DependencyProperty)
            {
                return binding.ProvideValue(serviceProvider);
            }

            // Contexto de design / SharedDp: retorna string imediata
            return LocalizationService.Instance.GetString(Key);
        }
    }
}
