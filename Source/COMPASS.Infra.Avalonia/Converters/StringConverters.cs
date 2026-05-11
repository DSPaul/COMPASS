using Avalonia.Data.Converters;

namespace COMPASS.Infra.Avalonia.Converters;

public static partial class StringConverters
{
    public static FuncValueConverter<string?, string?> ToUpperConverter { get; } =
        new (value => value?.ToUpper());
}