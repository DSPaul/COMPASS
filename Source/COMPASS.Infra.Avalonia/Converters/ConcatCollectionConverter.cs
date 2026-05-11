using System.Collections;
using System.Globalization;
using Avalonia.Data.Converters;

namespace COMPASS.Infra.Avalonia.Converters
{
    public class CollectionUnionConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            return values
                .OfType<IEnumerable>()
                .SelectMany(e => e.Cast<object>())
                .Distinct()
                .ToList();
        }
    }
}
