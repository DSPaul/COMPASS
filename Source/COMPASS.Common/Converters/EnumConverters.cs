using System;
using Avalonia.Data.Converters;
using COMPASS.Common.Localization;

namespace COMPASS.Common.Converters;

public static partial class EnumConverters
{
    public static FuncValueConverter<Enum, string> LocalizationConverter { get; } = new(EnumLocalizer.Localize);
}