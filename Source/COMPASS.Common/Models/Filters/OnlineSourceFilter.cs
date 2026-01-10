namespace COMPASS.Common.Models.Filters
{
    internal class OnlineSourceFilter : Filter
    {
        public OnlineSourceFilter() : base(FilterType.OnlineSource)
        {
            RelatedProperties.Add(nameof(Codex.Sources));
        }

        public override bool Apply(Codex codex) => codex.Sources.HasOnlineSource();
    }
}
