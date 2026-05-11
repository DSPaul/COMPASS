using System.Globalization;
using Avalonia.Data.Converters;

namespace COMPASS.Infra.Avalonia.Converters;

/// <summary>
/// A converter that compares two integers and returns true if the first number is smaller or equal to the second number
/// </summary>
public class IsSmallerOrEqualConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2)
        {
            throw new ArgumentException("Expected exactly two numbers");
        }
        var firstNumber = values[0] as int?;
        var secondNumber = values[1] as int?;

        if (firstNumber == null)
        {
            throw new ArgumentException("The first value was not an integer");
        }

        if (secondNumber == null)
        {
            throw new ArgumentException("The second value was not an integer");
        }

        return firstNumber <= secondNumber;
    }
}