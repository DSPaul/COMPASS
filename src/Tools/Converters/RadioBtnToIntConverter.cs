using System;
using System.Globalization;
using System.Windows.Data;

namespace COMPASS.Tools.Converters
{
    public class RadioBtnToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) 
            => int.Parse((string)parameter) == (int)value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
            => (bool)value ? parameter : null;
    }
}
