using System.Text.RegularExpressions;

namespace COMPASS.Common.Features.ISBN;

public static partial class Patterns
{
    //ISBN expressions
    [GeneratedRegex(@"(978|979)[- ]?\d{1,5}[- ]?\d{1,7}[- ]?\d{1,6}[- ]?\d")]
    public static partial Regex ISBN();
}
