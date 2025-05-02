using Avalonia.Data.Converters;
using Avalonia.Media;

namespace COMPASS.Common.Converters
{
    public static class ColorConverters
    {
        public static FuncValueConverter<Color, SolidColorBrush> ColorToBrush { get; } = 
            new (color => new SolidColorBrush(color));
    }
}
