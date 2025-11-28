namespace COMPASS.Common.Models.Filters
{
    internal class DomainFilter : Filter
    {
        public DomainFilter(string domain) : base(FilterType.Domain, domain)
        {
            AllowMultiple = true;
        }

        public override bool Apply(Codex codex) =>
            FilterValue is string domain &&
            codex.Sources.HasOnlineSource() &&
            codex.Sources.SourceURL.Contains(domain);
    }
}
