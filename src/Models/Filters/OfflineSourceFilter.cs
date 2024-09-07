using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    public class OfflineSourceFilter : Filter
    {
        public OfflineSourceFilter() : base(FilterType.OfflineSource)
        { }

        public override string Content => "Available Offline";
        public override Color BackgroundColor => Colors.DarkSeaGreen;
        public override bool Apply(Codex codex) => codex.HasOfflineSource();
    }
}
