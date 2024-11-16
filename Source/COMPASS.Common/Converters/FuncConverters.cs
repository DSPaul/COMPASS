using Avalonia.Data.Converters;
using Avalonia.Media;

namespace COMPASS.Common.Converters
{
    public static class FuncConverters
    {
        public static FuncValueConverter<object?, object?, bool> AreEqual { get; } =
           new FuncValueConverter<object?, object?, bool>(areEqual);

        private static bool areEqual(object? value, object? param) => value?.Equals(param) ?? false;

        public static FuncValueConverter<Color, SolidColorBrush> ColorToBrush { get; } =
            new FuncValueConverter<Color, SolidColorBrush>(color => new(color));

    }
}
