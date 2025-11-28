using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.Models.Filters
{
    internal class TagFilter : Filter
    {
        public TagFilter(TagViewModel tagVm) : base(FilterType.Tag, tagVm)
        {
            AllowMultiple = true;
        }

        //Tag logic is contained in the FiltersViewmodel, so here just make it match everything
        public override bool Apply(Codex codex) => true;
    }
}
