namespace COMPASS.Common.Models.Filters
{
    internal class MinimumRatingFilter : Filter
    {
        public MinimumRatingFilter(int minRating) : base(FilterType.MinimumRating, minRating)
        { }
        
        public override bool Apply(Codex codex) => FilterValue is int rating && codex.Rating >= rating;
    }
}
