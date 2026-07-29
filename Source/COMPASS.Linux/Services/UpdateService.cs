using COMPASS.ApiClients.GitHub;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models.Updates;
using System.Diagnostics;

namespace COMPASS.Linux.Services
{
    public class UpdateService(
        ILogger logger,
        IGitHubApiClient gitHubApiClient,
        INotificationService notificationService,
        IPreferencesService preferencesService,
        IWebService webService) : UpdateServiceBase(logger, gitHubApiClient, notificationService, preferencesService, webService)
    {
        protected override Task<bool> AssureUpdateDownloaded(Update update) => Task.FromResult(true); //nothing to download

        public override Task HandleUpdate(Update update)
        {
            //Just open the download page in the browser for Linux, since we can't auto-update
            //When we get on some package managers, we can tell users to go use those instead
            Process.Start(new ProcessStartInfo(update.ReleaseUrl) { UseShellExecute = true });
            return Task.CompletedTask;
        }
    }
}
