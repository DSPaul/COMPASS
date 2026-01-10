namespace COMPASS.Common.Models.Filters
{
    internal class PhysicalSourceFilter : Filter
    {
        public PhysicalSourceFilter() : base(FilterType.PhysicalSource)
        {
            RelatedProperties.Add(nameof(Codex.PhysicallyOwned));
        }
        
        public override bool Apply(Codex codex) => codex.PhysicallyOwned;
    }
}