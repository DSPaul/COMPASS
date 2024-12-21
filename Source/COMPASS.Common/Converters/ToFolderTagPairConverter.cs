using Avalonia.Data.Converters;
using COMPASS.Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace COMPASS.Common.Converters
{
    public class ToFolderTagPairConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) 
            => values is [string folder, Tag tag] ? new FolderTagPair(folder, tag) : null;
    }
}
