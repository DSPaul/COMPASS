using System;
using System.Globalization;
using System.Windows.Data;

namespace COMPASS.Converters
{
    internal class NegateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool b)
            {
                throw new ArgumentException($"{nameof(value)} was not a boolean");
            }
            return !b;
        }

        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
        {
            if (value is not bool b)
            {
                throw new ArgumentException($"{nameof(value)} was not a boolean");
            }
            return !b;
        }
    }
}
