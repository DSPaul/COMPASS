using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class OnlineSourceFilter : Filter
    {
        public OnlineSourceFilter() : base(FilterType.OnlineSource)
        { }

        public override bool Apply(Codex codex) => codex.HasOnlineSource();

        public override string Content => "Available Online";
        public override Color BackgroundColor => Colors.DarkSeaGreen;
    }
}
