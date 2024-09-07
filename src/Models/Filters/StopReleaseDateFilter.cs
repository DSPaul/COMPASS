using System;
using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class StopReleaseDateFilter : Filter
    {
        public StopReleaseDateFilter(DateTime date) : base(FilterType.StopReleaseDate, date)
        { }

        public override string Content => $"Before: {(FilterValue as DateTime?)?.ToShortDateString()}";
        public override Color BackgroundColor => Colors.DeepSkyBlue;
        public override bool Apply(Codex codex) => FilterValue is DateTime date && codex.ReleaseDate < date;
    }
}
