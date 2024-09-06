using System;
using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    public class StartReleaseDateFilter : FilterBase
    {
        public StartReleaseDateFilter(DateTime date) : base(FilterType.StartReleaseDate, date)
        { }

        public override string Content => $"After: {(FilterValue as DateTime?)?.ToShortDateString()}";
        public override Color BackgroundColor => Colors.DeepSkyBlue;
        public override bool Apply(Codex codex) => FilterValue is DateTime date && codex.ReleaseDate >= date;
    }
}
