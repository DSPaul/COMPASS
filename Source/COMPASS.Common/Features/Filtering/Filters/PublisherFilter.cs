using COMPASS.Common.Domain;
namespace COMPASS.Common.Features.Filtering.Filters
{
    public class PublisherFilter : Filter
    {
        public PublisherFilter(string publisher) : base(FilterType.Publisher, publisher)
        {
            AllowMultiple = true;
            RelatedProperties.Add(nameof(Codex.Publisher));   
        }
        
        public override bool Apply(Codex codex) => FilterValue is string publisher && codex.Publisher == publisher;
    }
}
