using Avalonia.Data.Converters;

namespace COMPASS.Infra.Converters;

public static class ObjectConverters
{
    public static FuncValueConverter<object?, object?, bool> AreEqual { get; } = 
        new((value, param) => value?.Equals(param) ?? false);
}