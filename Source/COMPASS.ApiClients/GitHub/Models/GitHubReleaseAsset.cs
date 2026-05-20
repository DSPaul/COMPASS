using System.Text.Json.Serialization;

namespace COMPASS.ApiClients.GitHub.Models
{
    public class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; init; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; init; }

        [JsonPropertyName("download_count")]
        public int DownloadCount { get; init; }

        [JsonPropertyName("content_type")]
        public string ContentType { get; init; } = string.Empty;
    }
}
