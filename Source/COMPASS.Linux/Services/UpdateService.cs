using COMPASS.ApiClients.GitHub;
using COMPASS.Infra.Application;
using COMPASS.Infra.IO;
using COMPASS.Infra.Logging;
using COMPASS.Infra.Notifications;
using COMPASS.Infra.Preferences;
using COMPASS.Infra.Updates;
using COMPASS.Infra.Web;
using System.Diagnostics;
using COMPASS.Common.Domain;

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

        private const string FLATHUB_URL = "https://flathub.org/en-GB/apps/info.compassapp.COMPASS";
        private const string FLATPAK_UPDATE_COMMAND = "flatpak update info.compassapp.COMPASS";

        public override async Task HandleUpdate(Update update)
        {
            //COMPASS is distributed through Flathub on Linux, so updates go through flatpak
            var notification = new Notification(
                "Update via Flathub",
                $"COMPASS on Linux is updated through Flathub. To install v{update.Version}, run the following command in a terminal:\n\n{FLATPAK_UPDATE_COMMAND}\n\nor update it through your software center.",
                Severity.Info,
                NotificationAction.Cancel | NotificationAction.Confirm)
            {
                ConfirmText = "Open Flathub",
                CancelText = "Close"
            };

            await notificationService.ShowDialog(notification).ConfigureAwait(false);

            if (notification.Result == NotificationAction.Confirm)
            {
                Process.Start(new ProcessStartInfo(FLATHUB_URL) { UseShellExecute = true });
            }
        }
    }
}
