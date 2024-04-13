using Avalonia.Data;
using Avalonia.Data.Converters;
using COMPASS.Common.Models;
using System;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    public class PropIsEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null)
            {
                var exception = new ArgumentNullException(nameof(value));
                return new BindingNotification(exception, BindingErrorType.Error);
            }

            if (parameter == null)
            {
                var exception = new ArgumentNullException(nameof(parameter));
                return new BindingNotification(exception, BindingErrorType.Error);
            }
            CodexProperty? prop = Codex.Properties.Find(prop => prop.Name == parameter.ToString());

            if (prop is null)
            {
                var exception = new ArgumentException($"Property {parameter} could not be found");
                return new BindingNotification(exception, BindingErrorType.Error);
            }

            return !prop.IsEmpty((Codex)value);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
