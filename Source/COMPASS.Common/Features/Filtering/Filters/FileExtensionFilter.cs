using COMPASS.Common.Domain;
namespace COMPASS.Common.Features.Filtering.Filters
{
    public class FileExtensionFilter : Filter
    {
        public FileExtensionFilter(string extension) : base(FilterType.FileExtension, extension)
        {
            AllowMultiple = true;
            RelatedProperties.Add(nameof(Codex.Sources));
        }

        public override bool Apply(Codex codex) => FilterValue is string extension && codex.Sources.FileType == extension;
    }
}
