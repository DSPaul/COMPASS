using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace COMPASS.Common.Converters
{
    public static class ColorConverters
    {
        public static FuncValueConverter<Color, SolidColorBrush> ColorToBrush { get; } = 
            new (color => new SolidColorBrush(color));

        public static FuncValueConverter<SolidColorBrush, string, SolidColorBrush> AddTransparency { get; } =
            new((brush, transparency) => new SolidColorBrush(Color.FromArgb((byte)(brush!.Color.A * float.Parse(transparency ?? "1", CultureInfo.InvariantCulture)), brush.Color.R, brush.Color.G, brush.Color.B)));
    }
}
