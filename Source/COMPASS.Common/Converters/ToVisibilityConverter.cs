using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    public class ToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? shouldInvert, CultureInfo culture)
        {
            bool invert = System.Convert.ToBoolean(shouldInvert);
            bool visible;

            if (value is string strValue)
            {
                visible = !String.IsNullOrEmpty(strValue);
            }
            else
            {
                try
                {
                    visible = System.Convert.ToBoolean(value);
                }
                catch
                {
                    visible = value is not null;
                }
            }
            return visible ^ invert;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
