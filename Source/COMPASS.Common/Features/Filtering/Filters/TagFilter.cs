using COMPASS.Common.Features.Tags;
using COMPASS.Common.Domain;

namespace COMPASS.Common.Features.Filtering.Filters
{
    internal class TagFilter : Filter
    {
        public TagFilter(TagViewModel tagVm) : base(FilterType.Tag, tagVm)
        {
            AllowMultiple = true;
            RelatedProperties.Add(nameof(Codex.Tags));
        }

        //Tag logic is contained in the FiltersViewmodel, so here just make it match everything
        public override bool Apply(Codex codex) => true;
    }
}
