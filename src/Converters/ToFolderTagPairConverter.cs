using COMPASS.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace COMPASS.Converters
{
    class ToFolderTagPairConverter : IMultiValueConverter
    {
        public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not null && values.Length == 2 && values[0] is string folder && values[1] is Tag tag)
            {
                return new FolderTagPair(folder, tag);
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
