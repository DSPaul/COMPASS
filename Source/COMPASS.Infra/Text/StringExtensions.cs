using FuzzySharp;
using System.Globalization;
using System.Text;

namespace COMPASS.Infra.Text;

public static class StringExtensions
{
    extension(string text)
    {
        public string PadNumbers(int totalWidth = 8)
        {
            if (String.IsNullOrEmpty(text)) return text;
            return RegexConstants.Numbers().Replace(text, match => match.Value.PadLeft(totalWidth, '0'));
        }

        /// <summary>
        /// Appends a running nr after the string until the string is valid
        /// </summary>
        /// <param name="validator"></param>
        /// <param name="maxAttempts"></param>
        /// <returns></returns>
        public string GetValidVariant(Func<string, bool> validator, int maxAttempts = 100)
        {
            if (validator(text)) return text;

            for (int i = 1; i <= maxAttempts; i++)
            {
                string candidate = $"{text} ({i})";
                if (validator(candidate)) return candidate;
            }

            //return original if not found a valid variant after maxAttempts
            //if the validity was crucial caller can simply call the validator on the return value to differentiate
            return text; 
        }

        public string RemoveDiacritics() =>
            //"héllo" becomes "he<acute>llo", which in turn becomes "hello".
            string.Concat(text.Normalize(NormalizationForm.FormD).Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)).Normalize(NormalizationForm.FormC);
    }

    extension(string? input)
    {
        public bool MatchesFuzzy(string pattern, int threshold = 80)
        {
            if (string.IsNullOrEmpty(pattern)) return true; //an empty pattern matches everything
            if (string.IsNullOrEmpty(input)) return false;

            string loweredInput = input.ToLower();
            string loweredPattern = pattern.ToLower();
            return Fuzz.TokenInitialismRatio(loweredInput, loweredPattern) > threshold || //include acronyms
                    input.Contains(pattern, StringComparison.CurrentCultureIgnoreCase) || //include string fragments
                    Fuzz.PartialRatio(loweredInput, loweredPattern) > 80; //include spelling errors
        }
    }
}
