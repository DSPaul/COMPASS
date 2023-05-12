using COMPASS.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace COMPASS.Converters
{
    public class PropIsEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            CodexProperty prop = Codex.Properties.FirstOrDefault(prop => prop.Label == parameter.ToString());
            bool empty = prop.IsEmpty((Codex)value);
            if (empty)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
