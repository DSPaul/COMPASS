using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace COMPASS.Common.Converters;

public class AreEqualConverter : IMultiValueConverter
{
    public bool Invert { get; set; }
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
        {
            return false;
        }

        if (values[0] == null || values[1] == null)
        {
            return false;
        }

        bool areEqual = values[0]!.Equals(values[1]);
        
        return Invert ? !areEqual : areEqual;
    }
}