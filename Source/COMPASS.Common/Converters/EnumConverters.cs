using System;
using Avalonia.Data.Converters;
using COMPASS.Common.Localization;

namespace COMPASS.Common.Converters;

public static class EnumConverters
{
    public static FuncValueConverter<Enum, Enum, bool> HasFlagConverter { get; } =
        new((value, param) => value is Enum flags && param is Enum flag && flags.HasFlag(flag));

    public static FuncValueConverter<Enum, string> LocalizationConverter { get; } = new(EnumLocalizer.Localize);
}