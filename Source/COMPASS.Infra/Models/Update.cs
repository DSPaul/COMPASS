using NuGet.Versioning;

namespace COMPASS.Infra.Models
{
    public struct Update
    {
        public Update(SemanticVersion version, string? downloadUrl, string? changeLog)
        {
            Version = version;
            DownloadUrl = downloadUrl ?? "";
            ReleaseNote = changeLog ?? "";
        }

        public SemanticVersion Version { get; set; }

        public string DownloadUrl { get; set; }

        public string ReleaseNote { get; set; }
    }
}
