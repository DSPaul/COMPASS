using Avalonia.Data.Converters;

namespace COMPASS.Common.Converters;

public static class IntConverters
{
    public static FuncValueConverter<int, string, bool> IsGreaterThanConverter = 
        new FuncValueConverter<int,string, bool>((value, param ) => value > int.Parse(param ?? "0"));
    
    public static FuncValueConverter<int, string, bool> IsLesserThanConverter = 
        new FuncValueConverter<int,string, bool>((value, param ) => value < int.Parse(param ?? "0"));
}