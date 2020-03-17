using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    if (value.GetType() == typeof(bool)) return Visibility.Visible;
                    break;

                case "Number":
                    if (value.GetType() == typeof(float) || value.GetType() == typeof(int) || value.GetType() == typeof(double)) return Visibility.Visible;
                    break;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
