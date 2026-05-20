using System.Text.Json;
using COMPASS.ApiClients.GitHub.Models;

namespace COMPASS.ApiClients.GitHub
{
    internal class GitHubApiClient(IHttpClientFactory httpClientFactory) : IGitHubApiClient
    {
        internal const string HttpClientName = "github";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Get a list of all releases from a github repo
        /// </summary>
        /// <param name="repoName">format "user/repo"</param>
        /// <returns></returns>
        public async Task<List<GitHubRelease>> GetReleasesAsync(string repoName)
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            var apiUrl = $"https://api.github.com/repos/{repoName}/releases";
            var json = await client.GetStringAsync(apiUrl).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<GitHubRelease>>(json, _jsonOptions) ?? [];
        }
    }
}

