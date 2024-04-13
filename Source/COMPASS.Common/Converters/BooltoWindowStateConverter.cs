using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    public class WindowStateToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value != null && (WindowState)value == WindowState.Maximized;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value != null && (bool)value ? WindowState.Maximized : WindowState.Normal;
    }
}
