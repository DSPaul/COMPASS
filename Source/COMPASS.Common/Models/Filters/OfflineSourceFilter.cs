namespace COMPASS.Common.Models.Filters
{
    public class OfflineSourceFilter : Filter
    {
        public OfflineSourceFilter() : base(FilterType.OfflineSource)
        { }
        public override bool Apply(Codex codex) => codex.Sources.HasOfflineSource();
    }
}
