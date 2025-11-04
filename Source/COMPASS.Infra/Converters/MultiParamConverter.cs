using System.Globalization;
using Avalonia.Data.Converters;

namespace COMPASS.Infra.Converters
{
    public class MultiParamConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            => values.ToList();
    }
}
