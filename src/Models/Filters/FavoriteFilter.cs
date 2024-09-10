using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class FavoriteFilter : Filter
    {

        public FavoriteFilter() : base(FilterType.Favorite)
        { }

        public override Color BackgroundColor => Colors.HotPink;

        public override string Content => "Favorite";

        public override bool Apply(Codex codex) => codex.Favorite;
    }
}
