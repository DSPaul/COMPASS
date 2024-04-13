using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace COMPASS.Converters
{
    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 2 || values[0] == null)
            {
                return false;
            }

            return values[0]!.Equals(values[1]);
        }
    }
}
