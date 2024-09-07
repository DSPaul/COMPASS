using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    public class PublisherFilter : Filter
    {
        public PublisherFilter(string publisher) : base(FilterType.Publisher, publisher)
        {
            AllowMultiple = true;
        }


        public override string Content => $"Publisher: {FilterValue}";
        public override Color BackgroundColor => Colors.MediumPurple;
        public override bool Apply(Codex codex) => FilterValue is string publisher && codex.Publisher == publisher;
    }
}
