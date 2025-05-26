using System;
using Avalonia.Data.Converters;
using COMPASS.Common.Localization;

namespace COMPASS.Common.Converters;

public static class StringConverters
{
    public static FuncValueConverter<string?, string?> ToUpperConverter { get; } =
        new (value => value?.ToUpper());
    
    public static FuncValueConverter<object?, string?> ToStringConverter { get; } = 
        new (value => value switch
        {
            Enum e => EnumLocalizer.Localize(e),
            string s => s,
            _ => value?.ToString()
        });
}