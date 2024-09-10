using Avalonia.Media;

namespace COMPASS.Common.Models.Filters
{
    public class FileExtensionFilter : Filter
    {
        public FileExtensionFilter(string extension) : base(FilterType.FileExtension, extension)
        {
            AllowMultiple = true;
        }

        public override Color BackgroundColor => Colors.OrangeRed;

        public override string Content => $"File Type: {FilterValue}";

        public override bool Apply(Codex codex) => FilterValue is string extension && codex.Sources.FileType == extension;
    }
}
