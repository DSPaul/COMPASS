using Avalonia.Data.Converters;
using COMPASS.Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    class ToFolderTagPairConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not null && values.Count == 2 && values[0] is string folder && values[1] is Tag tag)
            {
                return new FolderTagPair(folder, tag);
            }
            return null;
        }
    }
}
