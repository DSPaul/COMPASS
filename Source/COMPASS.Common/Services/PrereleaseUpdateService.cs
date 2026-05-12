using COMPASS.Common.Models;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;
using System.Text.Json.Nodes;
using NuGet.Versioning;
using System.Diagnostics;

namespace COMPASS.Common.Services
{
    public class PrereleaseUpdateService : IUpdateService
    {
        private static readonly HttpClient _httpClient = new();

        static PrereleaseUpdateService()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("COMPASS");
        }

        public async Task<List<Update>> CheckForUpdates()
        {
            var apiUrl = Constants.RepoURL.Replace("https://github.com/", "https://api.github.com/repos/") + "/releases";
            var json = await _httpClient.GetStringAsync(apiUrl).ConfigureAwait(false);
            var releases = JsonNode.Parse(json)?.AsArray() ?? [];

            SemanticVersion currentVersion = SemanticVersion.Parse(ApplicationService.Version);

            List<Update> updates = new();

            foreach (var release in releases)
            {
                var tagName = release?["tag_name"]?.GetValue<string>()?.TrimStart('v');
                if (tagName is not null &&
                    SemanticVersion.TryParse(tagName, out var version) &&
                    version > currentVersion)
                {
                    string? url = release?["html_url"]?.GetValue<string>();
                    string? releaseNotes = release?["body"]?.GetValue<string>();
                    updates.Add(new Update(version, url, releaseNotes));
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
