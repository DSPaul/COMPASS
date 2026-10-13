using COMPASS.Common.Domain;
namespace COMPASS.Common.Features.Filtering.Filters
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
