using System.IO;

namespace COMPASS.Common.Models.Filters
{
    internal class HasBrokenPathFilter : Filter
    {
        public HasBrokenPathFilter() : base(FilterType.HasBrokenPath)
        { }

        public override bool Apply(Codex codex) =>
            codex.Sources.HasOfflineSource() && !Path.Exists(codex.Sources.Path);
    }
}
