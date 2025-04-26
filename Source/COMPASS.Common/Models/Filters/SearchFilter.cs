using Avalonia.Media;
using FuzzySharp;
using System;
using COMPASS.Common.Tools;

namespace COMPASS.Common.Models.Filters
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

            return !string.IsNullOrWhiteSpace(searchTerm) && codex.Title.MatchesFuzzy(searchTerm);
        }
    }
}
