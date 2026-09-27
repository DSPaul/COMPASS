using COMPASS.ApiClients.GitHub;
using COMPASS.Common.Models;
using COMPASS.Infra.Application;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Notifications;
using COMPASS.Infra.Preferences;
using COMPASS.Infra.Updates;
using COMPASS.Infra.Web;

namespace COMPASS.Windows.Services
{
    internal class UpdateService(
        IApplicationService applicationService,
        IIOService ioService,
        ILogger logger,
        IGitHubApiClient gitHubApiClient,
        INotificationService notificationService,
        IPreferencesService preferencesService,
        IWebService webService) : UpdateServiceBase(applicationService, ioService, logger, gitHubApiClient, notificationService, preferencesService, webService)
    {
        protected override string RepoName => Constants.GITHUB_REPO_NAME;

        protected override async Task<bool> AssureUpdateDownloaded(Update update)
        {
            var innoInstallerAsset = GetInnoInstallerAsset(update);
            if (innoInstallerAsset != null)
            {
                var installerPath = await AssureAssetDownloaded(innoInstallerAsset.Value);
                return File.Exists(installerPath);
            }
            return false;
        }

        public override async Task HandleUpdate(Update update)
        {
            bool downloaded = await AssureUpdateDownloaded(update);
            var innoInstallerAsset = GetInnoInstallerAsset(update);
            if (downloaded && innoInstallerAsset != null)
            {
                await InstallReleaseAsset(innoInstallerAsset.Value, "/SILENT");
            }
        }

        private ReleaseAsset? GetInnoInstallerAsset(Update update)
        {
            string innoInstallerName = $"COMPASS_Setup_{update.Version}.exe";
            return update.Assets.FirstOrDefault(asset => asset.AssetName == innoInstallerName);
        }
    }
}
