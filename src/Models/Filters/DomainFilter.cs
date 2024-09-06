using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class DomainFilter : FilterBase
    {
        public DomainFilter(string domain) : base(FilterType.Domain, domain)
        {
            AllowMultiple = true;
        }

        public override Color BackgroundColor => Colors.MediumTurquoise;

        public override string Content => $"From: {FilterValue}";

        public override bool Apply(Codex codex) => FilterValue is string domain && codex.HasOnlineSource() && codex.SourceURL.Contains(domain);
    }
}
