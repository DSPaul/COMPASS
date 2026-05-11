using System.Globalization;
using Avalonia.Data.Converters;

namespace COMPASS.Infra.Avalonia.Converters;

/// <summary>
/// Converter to bind enum value to radio buttons
/// </summary>
public class EnumForRadioBtnConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null when parameter is null => true,
            null => false,
            _ => value.Equals(parameter)
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isChecked = (bool)value!;
        return isChecked ?  parameter : null;
    }
}