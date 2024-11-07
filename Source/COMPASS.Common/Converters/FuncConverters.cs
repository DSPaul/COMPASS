using Avalonia.Data.Converters;

namespace COMPASS.Common.Converters
{
    public static class FuncConverters
    {
        public static FuncValueConverter<object?, object?, bool> AreEqual { get; } =
           new FuncValueConverter<object?, object?, bool>(areEqual);

        private static bool areEqual(object? value, object? param) => value?.Equals(param) ?? false;
    }
}
