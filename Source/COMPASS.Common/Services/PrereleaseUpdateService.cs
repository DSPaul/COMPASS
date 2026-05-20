using COMPASS.ApiClients.GitHub;
using COMPASS.Common.Models;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;
using NuGet.Versioning;
using System.Diagnostics;

namespace COMPASS.Common.Services
{
    public class PrereleaseUpdateService(IGitHubApiClient gitHubApiClient) : IUpdateService
    {
        public async Task<List<Update>> CheckForUpdates()
        {
            var releases = await gitHubApiClient.GetReleasesAsync(Constants.RepoName).ConfigureAwait(false);

            SemanticVersion currentVersion = SemanticVersion.Parse(ApplicationService.Version);

            List<Update> updates = new();

            foreach (var release in releases)
            {
                var tagName = release.TagName.TrimStart('v');
                if (SemanticVersion.TryParse(tagName, out var version) && version > currentVersion)
                {
                    updates.Add(new Update(version, release.HtmlUrl, release.Body));
                }
            }

            return updates;
        }

        public async Task HandleUpdates(IList<Update> updates)
        {
            if(updates.Count == 0)
                return;

            var latest = updates.OrderBy(u => u.Version).Last();

            string changelog = string.Join("\n", 
                updates.OrderByDescending(u => u.Version)
                       .Select(u => $"# v{u.Version} - {u.ReleaseNote.Trim("#")}"));

            var notificationService = ServiceResolver.Resolve<INotificationService>();
            var notification = new Notification(
                "Update Available",
                $"Version v{latest.Version} is available. \n" +
                $"Click download to go to the download page.",
                Severity.Info,
                NotificationAction.Confirm | NotificationAction.Cancel)
            {
                ConfirmText = "Download",
                CancelText = "Later",
                Details = changelog
            };

            await notificationService.ShowDialog(notification).ConfigureAwait(false);

            if (notification.Result == NotificationAction.Confirm)
            {
                Process.Start(new ProcessStartInfo(latest.DownloadUrl) { UseShellExecute = true });
            }
        }
    }
}
