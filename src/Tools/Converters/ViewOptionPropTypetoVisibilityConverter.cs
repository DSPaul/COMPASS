using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace COMPASS.Tools.Converters
{
    public class ViewOptionPropTypetoVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            switch (parameter)
            {
                case "Boolean":
                    if (value is bool) return Visibility.Visible;
                    break;

                case "Number":
                    if ((value is float) || (value is int) || (value is double)) return Visibility.Visible;
                    break;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
            => throw new NotImplementedException();
    }
}
