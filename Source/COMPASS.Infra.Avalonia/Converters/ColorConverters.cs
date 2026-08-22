using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace COMPASS.Infra.Avalonia.Converters
{
    public static class ColorConverters
    {
        public static FuncValueConverter<Color, SolidColorBrush> ColorToBrush { get; } = 
            new (color => new SolidColorBrush(color));

        public static FuncValueConverter<IBrush?, string, SolidColorBrush?> AddTransparency { get; } =
            new((brush, transparency) =>
            {
                if (brush is null)
                {
                    return null;
                }

                float addedTransparencey = float.Parse(transparency ?? "1", CultureInfo.InvariantCulture);

                switch (brush)
                {
                    case ISolidColorBrush solidBrush:
                        byte alpha = (byte)(solidBrush.Color.A * addedTransparencey);
                        return new SolidColorBrush(Color.FromArgb(alpha, solidBrush.Color.R, solidBrush.Color.G, solidBrush.Color.B));
                }

                return null;
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
