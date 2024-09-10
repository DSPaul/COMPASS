using Avalonia.Media;

namespace COMPASS.Common.Models.Filters
{
    internal class PhysicalSourceFilter : Filter
    {
        public PhysicalSourceFilter() : base(FilterType.PhysicalSource)
        { }

        public override string Content => "Physically Owned";
        public override Color BackgroundColor => Colors.DarkSeaGreen;
        public override bool Apply(Codex codex) => codex.PhysicallyOwned;
    }
}