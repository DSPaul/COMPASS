using COMPASS.Common.Domain;
namespace COMPASS.Common.Features.Filtering.Filters
{
    public class OfflineSourceFilter : Filter
    {
        public OfflineSourceFilter() : base(FilterType.OfflineSource)
        {
            RelatedProperties.Add(nameof(Codex.Sources));   
        }
        public override bool Apply(Codex codex) => codex.Sources.HasOfflineSource();
    }
}
