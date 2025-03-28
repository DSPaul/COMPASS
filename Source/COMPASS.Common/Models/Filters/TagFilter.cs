using Avalonia.Media;

namespace COMPASS.Common.Models.Filters
{
    internal class TagFilter : Filter
    {
        public TagFilter(Tag tag) : base(FilterType.Tag, tag)
        {
            AllowMultiple = true;
        }

        public override Color BackgroundColor => ((Tag)FilterValue!).BackgroundColor;

        public override string Content => ((Tag)FilterValue!).Name;

        //Tag logic is contained in the FilterViewmodel, so here just make it match everything
        public override bool Apply(Codex codex) => true;
    }
}
