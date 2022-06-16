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
    public class ToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Parameter is true if inverted
            bool Invert = System.Convert.ToBoolean(parameter);
            bool visible;
            if (value.GetType() == typeof(string)) visible = !string.IsNullOrEmpty((string)value);
            else visible = System.Convert.ToBoolean(value); //visible if true and no invert or false and invert -> XOR
            if (visible ^ Invert) return Visibility.Visible;
            else return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
