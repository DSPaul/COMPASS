namespace COMPASS.Common.Models.Filters
{
    public class PublisherFilter : Filter
    {
        public PublisherFilter(string publisher) : base(FilterType.Publisher, publisher)
        {
            AllowMultiple = true;
        }
        
        public override bool Apply(Codex codex) => FilterValue is string publisher && codex.Publisher == publisher;
    }
}
