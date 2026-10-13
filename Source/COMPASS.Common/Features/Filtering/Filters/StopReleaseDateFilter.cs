using COMPASS.Common.Domain;
namespace COMPASS.Common.Features.Filtering.Filters
{
    internal class StopReleaseDateFilter : Filter
    {
        public StopReleaseDateFilter(DateTime date) : base(FilterType.StopReleaseDate, date)
        {
            RelatedProperties.Add(nameof(Codex.ReleaseDate));   
        }
        
        public override bool Apply(Codex codex) => FilterValue is DateTime date && codex.ReleaseDate < date;
    }
}
