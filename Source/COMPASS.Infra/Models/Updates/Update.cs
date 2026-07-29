using NuGet.Versioning;

namespace COMPASS.Infra.Models.Updates
{
    public struct Update
    {
        public Update(SemanticVersion version, string? releaseUrl, string? changeLog)
        {
            Version = version;
            ReleaseUrl = releaseUrl ?? "";
            ReleaseNote = changeLog ?? "";
        }

        public SemanticVersion Version { get; }

        public string ReleaseUrl { get; }

        public string ReleaseNote { get; }

        public List<ReleaseAsset> Assets { get; } = [];
    }
}
