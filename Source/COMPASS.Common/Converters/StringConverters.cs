using Avalonia.Data.Converters;

namespace COMPASS.Common.Converters;

public static class StringConverters
{
    public static FuncValueConverter<string?, string?> ToUpperConverter { get; } =
        new (value => value?.ToUpper());
}