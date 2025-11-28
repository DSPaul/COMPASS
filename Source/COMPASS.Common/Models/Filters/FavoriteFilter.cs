namespace COMPASS.Common.Models.Filters
{
    internal class FavoriteFilter : Filter
    {

        public FavoriteFilter() : base(FilterType.Favorite)
        { }

        public override bool Apply(Codex codex) => codex.Favorite;
    }
}
