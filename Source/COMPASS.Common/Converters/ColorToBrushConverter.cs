using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    public class ColorToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is null ? null : new SolidColorBrush((Color)value);

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
