using COMPASS.ApiClients.GitHub;
using COMPASS.Common.Models;
using COMPASS.Infra.Application;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Notifications;
using COMPASS.Infra.Preferences;
using COMPASS.Infra.Updates;
using COMPASS.Infra.Web;
using System.Diagnostics;

namespace COMPASS.Linux.Services
{
    public class UpdateService(
        IApplicationService applicationService,
        IIOService ioService,
        ILogger logger,
        IGitHubApiClient gitHubApiClient,
        INotificationService notificationService,
        IPreferencesService preferencesService,
        IWebService webService) : UpdateServiceBase(applicationService, ioService, logger, gitHubApiClient, notificationService, preferencesService, webService)
    {
        protected override string RepoName => Constants.GITHUB_REPO_NAME;

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
