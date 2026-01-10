using System;
namespace COMPASS.Common.Models.Filters
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
