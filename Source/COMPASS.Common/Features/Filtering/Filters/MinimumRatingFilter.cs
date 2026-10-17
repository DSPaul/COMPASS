using COMPASS.Common.Domain;
namespace COMPASS.Common.Features.Filtering.Filters
{
    internal class MinimumRatingFilter : Filter
    {
        public MinimumRatingFilter(int minRating) : base(FilterType.MinimumRating, minRating)
        {
            RelatedProperties.Add(nameof(Codex.Rating));
        }
        
        public override bool Apply(Codex codex) => FilterValue is int rating && codex.Rating >= rating;
    }
}
