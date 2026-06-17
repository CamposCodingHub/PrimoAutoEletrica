using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PrimoAutoEletrica.Converters
{
    public class TendenciaToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tendencia)
            {
                return tendencia.ToLower() switch
                {
                    "crescimento" or "cresc" or "positivo" or "cima" or "up" => new SolidColorBrush(Color.FromRgb(74, 222, 128)), // #4ADE80
                    "queda" or "desc" or "negativo" or "baixo" or "down" => new SolidColorBrush(Color.FromRgb(248, 113, 113)), // #F87171
                    "estável" or "estavel" or "neutro" or "igual" => new SolidColorBrush(Color.FromRgb(148, 163, 184)), // #94A3B8
                    _ => new SolidColorBrush(Color.FromRgb(148, 163, 184)) // #94A3B8
                };
            }

            return new SolidColorBrush(Color.FromRgb(148, 163, 184)); // #94A3B8
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
