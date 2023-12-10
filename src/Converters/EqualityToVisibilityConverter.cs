using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace COMPASS.Converters
{

    /// <summary>
    /// Used to only show a control if the value equals the parameter
    /// Perfect for wizards that only show certain controls in certain steps of the process
    /// </summary>
    public class EqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool showOnEqual = true;
            // if parameter starts with '!' like "!value" then is should be inverted
            if (parameter is string s && s.StartsWith('!'))
            {
                showOnEqual = false;
                parameter = parameter.ToString()[1..];
            }
            return value?.Equals(parameter) == showOnEqual ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
