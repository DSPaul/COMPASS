using Avalonia.Data.Converters;
using System.Globalization;

namespace COMPASS.Infra.Avalonia.Converters
{
    public class MultiParamConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            => values.ToList();
    }
}
