namespace COMPASS.Common.Models.Filters
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
