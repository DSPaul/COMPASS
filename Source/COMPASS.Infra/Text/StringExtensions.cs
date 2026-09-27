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
