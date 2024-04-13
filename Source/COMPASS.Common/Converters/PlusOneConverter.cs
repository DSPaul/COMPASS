using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace COMPASS.Converters
{
    /// <summary>
    /// Converter that displays one more than the actual value, perfect for converting indices/counter that start at 0 to
    /// "normal" counting that starts at 1
    /// </summary>
    class PlusOneConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not int i)
            {
                var exception = new ArgumentException($"{nameof(value)} was not an integer");
                return new BindingNotification(exception, BindingErrorType.Error);
            }
            return i + 1;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not int i)
            {
                var exception = new ArgumentException($"{nameof(value)} was not an integer");
                return new BindingNotification(exception, BindingErrorType.Error);
            }
            return i - 1;
        }
    }
}
