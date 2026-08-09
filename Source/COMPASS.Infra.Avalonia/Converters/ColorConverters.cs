using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace COMPASS.Infra.Avalonia.Converters
{
    public static class ColorConverters
    {
        public static FuncValueConverter<Color, SolidColorBrush> ColorToBrush { get; } = 
            new (color => new SolidColorBrush(color));

        public static FuncValueConverter<SolidColorBrush?, string, SolidColorBrush?> AddTransparency { get; } =
            new((brush, transparency) =>
            {
                if (brush is null)
                {
                    return brush;
                }
                byte alpha = (byte)(brush.Color.A * float.Parse(transparency ?? "1", CultureInfo.InvariantCulture));
                return new SolidColorBrush(Color.FromArgb(alpha, brush.Color.R, brush.Color.G, brush.Color.B));
            });

        public static FuncValueConverter<Color?, string, SolidColorBrush?> ColorToTransparentBrush { get; } =
            new((color, transparency) =>
            {
                if(color is null)
                {
                    return null;
                }

                var brush = new SolidColorBrush(color.Value);
                byte alpha = (byte)(brush.Color.A * float.Parse(transparency ?? "1", CultureInfo.InvariantCulture));
                return new SolidColorBrush(Color.FromArgb(alpha, brush.Color.R, brush.Color.G, brush.Color.B));
            });
    }
}
