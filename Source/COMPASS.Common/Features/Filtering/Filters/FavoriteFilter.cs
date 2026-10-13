using COMPASS.Common.Domain;
namespace COMPASS.Common.Features.Filtering.Filters
{
    internal class FavoriteFilter : Filter
    {

        public FavoriteFilter() : base(FilterType.Favorite)
        {
            RelatedProperties.Add(nameof(Codex.Favorite));
        }

        public override bool Apply(Codex codex) => codex.Favorite;
    }
}
