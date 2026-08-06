using COMPASS.ApiClients.GitHub;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Models.Updates;
using COMPASS.Infra.Tools;
using NuGet.Versioning;
using System.Diagnostics;

namespace COMPASS.Common.Services
{
    public abstract class UpdateServiceBase(
        IIOService ioService,
        ILogger logger,
        IGitHubApiClient gitHubApiClient,
        INotificationService notificationService,
        IPreferencesService preferencesService,
        IWebService webService) : IUpdateService
    {
        #region IUpdateService Implementation

        public abstract Task HandleUpdate (Update update);

        public async Task<List<Update>> CheckForUpdates(bool includePrerelease)
        {
            var releases = await gitHubApiClient.GetReleasesAsync(Constants.RepoName).ConfigureAwait(false);

            SemanticVersion currentVersion = SemanticVersion.Parse(ApplicationService.Version);

            List<Update> updates = [];

            foreach (var release in releases)
            {
                var tagName = release.TagName.TrimStart('v');
                if (SemanticVersion.TryParse(tagName, out var version) && 
                    version > currentVersion && 
                    (includePrerelease || !version.IsPrerelease))
                {
                    var update = new Update(version, release.HtmlUrl, release.Body);
                    foreach (var asset in release.Assets)
                    {
                        update.Assets.Add(new ReleaseAsset(asset.Name, asset.BrowserDownloadUrl, asset.Checksum, asset.Size));
                    }

                    updates.Add(update);
                }
            }

            return updates;
        }

        public async Task OnUpdatesFound(IList<Update> updates)
        {
            if (updates.Count == 0)
            {
                return;
            }

            var latest = updates.OrderByDescending(u => u.Version).First();
            var notifiedUpdates = preferencesService.Preferences.UpdatePreferences.NotifiedUpdates;

            //download required assets in the background first for a smoother user experience
            await AssureUpdateDownloaded(latest);

            if(notifiedUpdates.Contains(latest.Version.ToString()))
            {
                return;
            }

            string changelog = string.Join("\n",
                updates.OrderByDescending(u => u.Version)
                       .Select(u => $"# v{u.Version} - {u.ReleaseNote.Trim("#")}"));

            var notification = new Notification(
                "Update Available",
                $"Version v{latest.Version} is available.",
                Severity.Info,
                NotificationAction.Cancel | NotificationAction.Confirm)
            {
                ConfirmText = "Update",
                CancelText = "Not now",
                Details = changelog
            };

            await notificationService.ShowDialog(notification).ConfigureAwait(false);
            foreach(var update in updates)
            {
                notifiedUpdates.Add(update.Version.ToString());
            }

            if(notification.Result == NotificationAction.Confirm)
            {
                await HandleUpdate(latest);
            }
        }

        #endregion

        protected abstract Task<bool> AssureUpdateDownloaded(Update update);

        protected async Task<string?> AssureAssetDownloaded(ReleaseAsset asset)
        {
            string assetPath = UpdateManager.GetAssetPath(asset);

            bool downloaded = File.Exists(assetPath);
            if (!downloaded)
            {
                downloaded = await DownloadReleaseAsset(asset, assetPath);
            }

            return downloaded ? assetPath : null;
        }

        protected async Task<bool> DownloadReleaseAsset(ReleaseAsset asset, string targetPath)
        {
            byte[] assetData = [];

            //cut off sha256:... prefix
            string checkSum = asset.Checksum.Split(":").Last();

            ioService.EnsureDirectoryExists(targetPath);
            try
            {
                await Utils.RetryAsync(3, async () =>
                {
                    assetData = await webService.DownloadFileAsync(asset.DownloadUrl);
                    if (!UpdateManager.IsChecksumCorrect(assetData, checkSum))
                    {
                        throw new Exception($"File {asset.AssetName} failed to download properly, checksum was not correct");
                    }
                });
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to download asset {asset.AssetName}", ex);
                return false;
            }

            await File.WriteAllBytesAsync(targetPath, assetData);
            return true;
        }

        protected async Task InstallReleaseAsset(ReleaseAsset asset, params string[] args)
        {
            string? downloadPath = await AssureAssetDownloaded(asset);
            if (File.Exists(downloadPath))
            {
                //Start update executable
                var startInfo = new ProcessStartInfo
                {
                    FileName = downloadPath,
                    Arguments = string.Join(" ", args),
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                ApplicationService.Shutdown();
            }
            else
            {
                var notification = new Notification(
                    "Update Failed",
                    $"Failed to download the update {asset.AssetName}. Please try again later.",
                    Severity.Error,
                    NotificationAction.Confirm);
                notificationService.Notify(notification);
            }
        }
    }
}
