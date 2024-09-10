using Avalonia.Media;

namespace COMPASS.Common.Models.Filters
{
    internal class MinimumRatingFilter : Filter
    {
        public MinimumRatingFilter(int minRating) : base(FilterType.MinimumRating, minRating)
        { }

        public override string Content => $"At least {FilterValue} stars";
        public override Color BackgroundColor => Colors.Goldenrod;
        public override bool Apply(Codex codex) => FilterValue is int rating && codex.Rating >= rating;
    }
}
