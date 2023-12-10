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
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }
            CodexProperty prop = Codex.Properties.FirstOrDefault(prop => prop.Label == parameter.ToString());

            if (prop == null)
            {
                throw new ArgumentException($"Property {parameter} could not be found");
            }
            
            bool empty = prop.IsEmpty((Codex)value);
            return empty ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) 
            => throw new NotImplementedException();
    }
}
