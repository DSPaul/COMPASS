using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class TagFilter : Filter
    {
        public TagFilter(Tag tag) : base(FilterType.Tag, tag)
        {
            AllowMultiple = true;
        }

        public override Color BackgroundColor => ((ITag)FilterValue!).BackgroundColor;

        public override string Content => ((ITag)FilterValue!).Content;

        //Tag logic is contained in the FilterViewmodel, so here just make it match everything
        public override bool Apply(Codex codex) => true;
    }
}
