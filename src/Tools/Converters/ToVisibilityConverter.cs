using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace COMPASS.Tools.Converters
{
    public class ToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Parameter is true if inverted
            bool Invert = System.Convert.ToBoolean(parameter);
            bool visible;

            if (value is string str_value)
                visible = !string.IsNullOrEmpty(str_value);
            else
                visible = System.Convert.ToBoolean(value);

            if (visible ^ Invert)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
