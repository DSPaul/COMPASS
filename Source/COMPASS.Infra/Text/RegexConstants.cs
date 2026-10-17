using System.Text.RegularExpressions;
namespace COMPASS.Infra.Text
{
    public static partial class RegexConstants
    {
        //Regex expressions
        [GeneratedRegex(@"\s+")]
        public static partial Regex Whitespace();


        [GeneratedRegex(@"\d+")]
        public static partial Regex Numbers();
    }
}