using Avalonia.Media;
using System;

namespace COMPASS.Common.Models.Filters
{
    internal class HasISBNFilter : Filter
    {
        public HasISBNFilter() : base(FilterType.HasISBN)
        { }

        public override Color BackgroundColor => Colors.DarkSeaGreen;
        public override string Content => "Has ISBN";
        public override bool Apply(Codex codex) => !String.IsNullOrEmpty(codex.Sources.ISBN);
    }
}
