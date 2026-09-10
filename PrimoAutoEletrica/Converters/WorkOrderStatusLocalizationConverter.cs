using System;
using System.Globalization;
using System.Windows.Data;
using PrimoAutoEletrica.Helpers;

namespace PrimoAutoEletrica.Converters
{
    /// <summary>Converte status interno de OS para texto localizado de apresentacao.</summary>
    public sealed class WorkOrderStatusLocalizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return WorkOrderStatusLocalizer.Display(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
