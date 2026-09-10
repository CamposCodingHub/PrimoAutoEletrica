using System;
using System.Globalization;
using System.Windows.Data;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Converters
{
    /// <summary>
    /// Formata um valor com chave de catálogo (ConverterParameter = key).
    /// </summary>
    public sealed class LocalizationFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = parameter as string;
            if (string.IsNullOrWhiteSpace(key))
            {
                return value?.ToString() ?? string.Empty;
            }

            return LocalizationService.Instance.GetString(key, value ?? string.Empty);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Placa de veículo com rótulo localizado; null => VehicleNotInformed / VehicleNotLinked via parameter.
    /// ConverterParameter: "Informed" | "Linked"
    /// </summary>
    public sealed class VehiclePlateLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var placa = value as string;
            if (string.IsNullOrWhiteSpace(placa))
            {
                var mode = parameter as string;
                return LocalizationService.Instance.GetString(
                    string.Equals(mode, "Linked", StringComparison.OrdinalIgnoreCase)
                        ? "VehicleNotLinked"
                        : "VehicleNotInformed");
            }

            return LocalizationService.Instance.GetString("VehiclePlateFormat", placa);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
