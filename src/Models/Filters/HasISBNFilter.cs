using System;
using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class HasISBNFilter : Filter
    {
        public HasISBNFilter() : base(FilterType.HasISBN)
        { }

        public override Color BackgroundColor => Colors.DarkSeaGreen;
        public override string Content => "Has ISBN";
        public override bool Apply(Codex codex) => !String.IsNullOrEmpty(codex.ISBN);
    }
}
