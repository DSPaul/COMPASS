using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    internal class NegateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool b)
            {
                var exception = new ArgumentException($"{nameof(value)} was not a boolean");
                return new BindingNotification(exception, BindingErrorType.Error);
            }
            return !b;
        }


        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool b)
            {
                var exception = new ArgumentException($"{nameof(value)} was not a boolean");
                return new BindingNotification(exception, BindingErrorType.Error);
            }
            return !b;
        }
    }
}
