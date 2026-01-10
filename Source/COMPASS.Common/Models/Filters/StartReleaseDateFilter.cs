using System;

namespace COMPASS.Common.Models.Filters
{
    public class StartReleaseDateFilter : Filter
    {
        public StartReleaseDateFilter(DateTime date) : base(FilterType.StartReleaseDate, date)
        {
            RelatedProperties.Add(nameof(Codex.ReleaseDate));   
        }
        
        public override bool Apply(Codex codex) => FilterValue is DateTime date && codex.ReleaseDate >= date;
    }
}
