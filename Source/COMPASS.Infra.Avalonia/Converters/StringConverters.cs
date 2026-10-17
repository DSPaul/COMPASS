using Avalonia.Data.Converters;
using COMPASS.Infra.Text;

namespace COMPASS.Infra.Avalonia.Converters;

public static class StringConverters
{
    public static FuncValueConverter<string?, string?> ToUpperConverter { get; } =
        new (value => value?.ToUpper());

    /// <summary>
    /// Converts any value to a string, using the display name for enum values.
    /// </summary>
    public static FuncValueConverter<object?, string?> ToStringConverter { get; } =
        new (value => value switch
        {
            Enum enumValue => enumValue.GetDisplayName(),
            string text => text,
            _ => value?.ToString()
        });
}
