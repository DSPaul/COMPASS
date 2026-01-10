namespace COMPASS.Common.Models.Filters
{
    internal class AuthorFilter : Filter
    {
        public AuthorFilter(string author) : base(FilterType.Author, author)
        {
            AllowMultiple = true;
            RelatedProperties.Add(nameof(Codex.Authors));
        }
        
        public override bool Apply(Codex codex) => FilterValue is string author && codex.Authors.Contains(author);
    }
}
