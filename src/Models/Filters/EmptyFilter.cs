using COMPASS.Models.CodexProperties;
using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class EmptyFilter : Filter
    {
        public EmptyFilter(CodexProperty filterValue) : base(FilterType.Empty, filterValue)
        {
        }

        private CodexProperty prop => (CodexProperty)FilterValue!;

        public override Color BackgroundColor => Colors.LightSlateGray;

        public override string Content => $"No value for '{prop.Label}'";

        public override bool Apply(Codex codex) => prop.IsEmpty(codex);
    }
}
