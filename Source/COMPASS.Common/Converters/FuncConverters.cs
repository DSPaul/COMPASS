using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace COMPASS.Common.Converters
{
    public static class FuncConverters
    {
        public static FuncValueConverter<object?, object?, bool> AreEqual { get; } = 
            new((value, param) => value?.Equals(param) ?? false);

        public static FuncValueConverter<Color, SolidColorBrush> ColorToBrush { get; } = 
            new (color => new SolidColorBrush(color));

        public static FuncValueConverter<Enum, Enum, bool> HasFlagConverter { get; } =
            new((value, param) => value is Enum flags && param is Enum flag && flags.HasFlag(flag));
    }
}
