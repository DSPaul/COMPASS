using COMPASS.Common.Domain;
namespace COMPASS.Common.Features.Filtering.Filters
{
    internal class HasBrokenPathFilter : Filter
    {
        public HasBrokenPathFilter() : base(FilterType.HasBrokenPath)
        {
            RelatedProperties.Add(nameof(Codex.Sources));
        }

        public override bool Apply(Codex codex) =>
            codex.Sources.HasOfflineSource() && !Path.Exists(codex.Sources.Path);
    }
}
