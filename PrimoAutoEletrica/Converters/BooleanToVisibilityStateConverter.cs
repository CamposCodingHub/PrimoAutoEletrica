using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimoAutoEletrica.Converters
{
    public class BooleanToVisibilityStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValue = value is bool current && current;
            var invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);

            if (invert)
            {
                boolValue = !boolValue;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
