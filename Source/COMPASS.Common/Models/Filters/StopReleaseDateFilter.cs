using System;
namespace COMPASS.Common.Models.Filters
{
    internal class StopReleaseDateFilter : Filter
    {
        public StopReleaseDateFilter(DateTime date) : base(FilterType.StopReleaseDate, date)
        { }
        
        public override bool Apply(Codex codex) => FilterValue is DateTime date && codex.ReleaseDate < date;
    }
}
