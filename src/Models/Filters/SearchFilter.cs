using FuzzySharp;
using System;
using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class SearchFilter : Filter
    {
        public SearchFilter(string searchTerm) : base(FilterType.Search, searchTerm)
        {
        }

        public override Color BackgroundColor => Colors.Salmon;

        public override string Content => $"Search: {FilterValue}";

        public override bool Apply(Codex codex)
        {
            string? searchTerm = FilterValue as string;

            if (string.IsNullOrWhiteSpace(searchTerm)) return false;

            return
                Fuzz.TokenInitialismRatio(codex.Title.ToLowerInvariant(), searchTerm) > 80 || //include acronyms
                codex.Title.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase) || //include string fragments
                Fuzz.PartialRatio(codex.Title.ToLowerInvariant(), searchTerm) > 80; //include spelling errors
        }
    }
}
