using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace COMPASS.Converters
{
    class FlagsToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Enum flags && parameter is Enum flag)
            {
                return flags.HasFlag(flag) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
