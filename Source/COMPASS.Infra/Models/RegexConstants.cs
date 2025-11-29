using System.Text.RegularExpressions;
namespace COMPASS.Infra.Models
{
    public static partial class RegexConstants
    {
        //Regex expressions
        [GeneratedRegex(@"(978|979)[- ]?\d{1,5}[- ]?\d{1,7}[- ]?\d{1,6}[- ]?\d")]
        public static partial Regex ISBN();

        [GeneratedRegex(@"\s+")]
        public static partial Regex Whitespace();


        [GeneratedRegex(@"\d+")]
        public static partial Regex NumbersOnly();
    }
}