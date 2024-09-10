using COMPASS.Models;
using COMPASS.Models.CodexProperties;
using System;
using System.Globalization;
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
            CodexProperty? prop = Codex.MedataProperties.Find(prop => prop.Name == parameter.ToString());

            if (prop is null)
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
