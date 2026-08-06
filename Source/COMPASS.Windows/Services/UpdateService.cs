using COMPASS.ApiClients.GitHub;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models.Updates;

namespace COMPASS.Windows.Services
{
    internal class UpdateService(
        IIOService ioService,
        ILogger logger,
        IGitHubApiClient gitHubApiClient,
        INotificationService notificationService,
        IPreferencesService preferencesService,
        IWebService webService) : UpdateServiceBase(ioService, logger, gitHubApiClient, notificationService, preferencesService, webService)
    {
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
