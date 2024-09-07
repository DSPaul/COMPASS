using System.IO;
using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class HasBrokenPathFilter : Filter
    {
        public HasBrokenPathFilter() : base(FilterType.HasBrokenPath)
        { }

        public override Color BackgroundColor => Colors.Gold;

        public override string Content => "Has broken path";

        public override bool Apply(Codex codex) => codex.HasOfflineSource() && !Path.Exists(codex.Path);
    }
}
