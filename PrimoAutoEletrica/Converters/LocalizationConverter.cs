using System;
using System.Globalization;
using System.Windows.Data;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Converters
{
    /// <summary>
    /// Conversor WPF para localização de strings
    /// Uso: Text="{Binding Key, Converter={StaticResource LocalizationConverter}}"
    /// </summary>
    public class LocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string key && !string.IsNullOrWhiteSpace(key))
            {
                return LocalizationService.Instance.GetString(key);
            }

            return value ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Não é necessário implementar reverse conversion para localização
            throw new NotImplementedException();
        }
    }
}
