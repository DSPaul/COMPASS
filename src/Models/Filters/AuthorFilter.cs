using System.Windows.Media;

namespace COMPASS.Models.Filters
{
    internal class AuthorFilter : Filter
    {
        public AuthorFilter(string author) : base(FilterType.Author, author)
        {
            AllowMultiple = true;
        }

        public override string Content => $"Author: {FilterValue}";
        public override Color BackgroundColor => Colors.Orange;
        public override bool Apply(Codex codex) => FilterValue is string author && codex.Authors.Contains(author);
    }
}
