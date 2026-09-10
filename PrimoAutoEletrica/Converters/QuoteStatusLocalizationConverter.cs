using System;
using System.Globalization;
using System.Windows.Data;
using PrimoAutoEletrica.Helpers;

namespace PrimoAutoEletrica.Converters
{
    public sealed class QuoteStatusLocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            if (parameter is string mode &&
                mode.Equals("Label", StringComparison.OrdinalIgnoreCase))
            {
                return QuoteStatusLocalizer.DisplayWithLabel(status);
            }

            return QuoteStatusLocalizer.Display(status);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
