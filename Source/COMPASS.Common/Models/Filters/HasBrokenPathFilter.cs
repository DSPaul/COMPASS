using Avalonia.Media;
using System.IO;

namespace COMPASS.Common.Models.Filters
{
    internal class HasBrokenPathFilter : Filter
    {
        public HasBrokenPathFilter() : base(FilterType.HasBrokenPath)
        { }

        public override Color BackgroundColor => Colors.Gold;

        public override string Content => "Has broken path";

        public override bool Apply(Codex codex) =>
            codex.Sources.HasOfflineSource() && !Path.Exists(codex.Sources.Path);
    }
}
