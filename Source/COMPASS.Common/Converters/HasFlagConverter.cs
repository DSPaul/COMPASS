using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    class HasFlagConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Enum flags && parameter is Enum flag)
            {
                return flags.HasFlag(flag);
            }
            return false;
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
