using COMPASS.Models;
using System;
using System.Globalization;
using System.Windows.Data;

namespace COMPASS.Converters
{
    class ToFolderTagPairConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            new FolderTagPair(values[0] as string, values[1] as Tag);
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
