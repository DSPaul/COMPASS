using COMPASS.Common.Features.Metadata;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Filtering.Filters
{
    internal class NotEmptyFilter : Filter
    {
        public NotEmptyFilter(CodexProperty filterValue) : base(FilterType.Empty, filterValue)
        {
            RelatedProperties.Add(filterValue.Name);
        }

        private CodexProperty prop => (CodexProperty)FilterValue!;

        public override bool Apply(Codex codex) => !prop.IsEmpty(codex);
    }
}
