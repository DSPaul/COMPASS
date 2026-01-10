using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.Models.Filters
{
    internal class SearchFilter : Filter
    {
        public SearchFilter(string searchTerm) : base(FilterType.Search, searchTerm)
        {
            RelatedProperties.Add(nameof(Codex.Title));   
        }

        public override bool Apply(Codex codex)
        {
            string? searchTerm = FilterValue as string;

            return !string.IsNullOrWhiteSpace(searchTerm) && codex.Title.MatchesFuzzy(searchTerm);
        }
    }
}
