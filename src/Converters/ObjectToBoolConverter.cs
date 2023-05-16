using System;
using System.Globalization;
using System.Windows.Data;

namespace COMPASS.Converters
{
    /// <summary>
    /// Converts any type into a bool,
    /// Convert: returns true if the param is equal to the bound prop
    /// ConvertBack: Sets the bound prop to the parameter if checked
    /// </summary>
    public class ObjectToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;
            return (bool)value ? parameter : null;
        }
    }

}
