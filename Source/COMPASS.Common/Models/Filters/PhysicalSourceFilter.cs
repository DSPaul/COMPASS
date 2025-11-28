namespace COMPASS.Common.Models.Filters
{
    internal class PhysicalSourceFilter : Filter
    {
        public PhysicalSourceFilter() : base(FilterType.PhysicalSource)
        { }
        
        public override bool Apply(Codex codex) => codex.PhysicallyOwned;
    }
}