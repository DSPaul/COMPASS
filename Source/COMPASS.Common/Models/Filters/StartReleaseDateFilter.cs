using Avalonia.Media;
using System;

namespace COMPASS.Common.Models.Filters
{
    public class StartReleaseDateFilter : Filter
    {
        public StartReleaseDateFilter(DateTime date) : base(FilterType.StartReleaseDate, date)
        { }

        public override string Content => $"After: {(FilterValue as DateTime?)?.ToShortDateString()}";
        public override Color BackgroundColor => Colors.DeepSkyBlue;
        public override bool Apply(Codex codex) => FilterValue is DateTime date && codex.ReleaseDate >= date;
    }
}
