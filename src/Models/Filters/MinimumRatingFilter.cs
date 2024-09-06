using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class MinimumRatingFilter : FilterBase
    {
        public MinimumRatingFilter(int minRating) : base(FilterType.MinimumRating, minRating)
        { }

        public override string Content => $"At least {FilterValue} stars";
        public override Color BackgroundColor => Colors.Goldenrod;
        public override bool Apply(Codex codex) => FilterValue is int rating && codex.Rating >= rating;
    }
}
