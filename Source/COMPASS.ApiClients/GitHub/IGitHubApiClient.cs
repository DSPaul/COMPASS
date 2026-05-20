using COMPASS.ApiClients.GitHub.Models;

namespace COMPASS.ApiClients.GitHub
{
    public interface IGitHubApiClient
    {
        public const string HttpClientName = "github";

        /// <summary>
        /// Get a list of all releases from a github repo
        /// </summary>
        /// <param name="repoName">format "user/repo"</param>
        Task<List<GitHubRelease>> GetReleasesAsync(string repoName);
    }
}
