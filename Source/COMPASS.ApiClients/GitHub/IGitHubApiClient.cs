using COMPASS.ApiClients.GitHub.Models;

namespace COMPASS.ApiClients.GitHub
{
    public interface IGitHubApiClient
    {
        /// <summary>
        /// Get a list of all releases from a github repo
        /// </summary>
        /// <param name="repoName">format "user/repo"</param>
        Task<List<GitHubRelease>> GetReleasesAsync(string repoName);
    }
}
