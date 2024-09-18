using COMPASS.Models.CodexProperties;
using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class NotEmptyFilter : Filter
    {
        public NotEmptyFilter(CodexProperty filterValue) : base(FilterType.Empty, filterValue)
        {
        }

        private CodexProperty prop => (CodexProperty)FilterValue!;

        public override Color BackgroundColor => Colors.LightSlateGray;

        public override string Content => $"Has value for '{prop.Label}'";

        public override bool Apply(Codex codex) => !prop.IsEmpty(codex);
    }
}
