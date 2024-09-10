using Avalonia.Media;

namespace COMPASS.Common.Models.Filters
{
    internal class OnlineSourceFilter : Filter
    {
        public OnlineSourceFilter() : base(FilterType.OnlineSource)
        { }

        public override bool Apply(Codex codex) => codex.Sources.HasOnlineSource();

        public override string Content => "Available Online";
        public override Color BackgroundColor => Colors.DarkSeaGreen;
    }
}
