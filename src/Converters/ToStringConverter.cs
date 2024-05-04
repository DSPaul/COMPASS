using System;
using System.Globalization;
using System.Windows.Data;

namespace COMPASS.Converters
{
    internal class ToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value as string ?? "";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
