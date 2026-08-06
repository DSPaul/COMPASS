using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Updates;
using System.Security.Cryptography;

namespace COMPASS.Common.Services.StateManagers
{
    public class UpdateManager(
        ILogger logger,
        IUpdateService updateService,
        IPreferencesService preferencesService,
        INotificationService notificationService)
    {
        private readonly CancellationTokenSource _cts = new();

        private readonly List<Update> _updates = [];

        public EventHandler<EventArgs>? OnUpdateFound;

        public bool UpdatesAvailable => _updates.Count > 0;

        public void StartUpdateCheckLoop()
        {
            if (preferencesService.Preferences.UpdatePreferences.CheckForUpdates) 
            { 
                _ = RunUpdateCheckLoop(_cts.Token);
            }
        }

        private void ClearUpdateAssets()
        {
            var updateFolder = Path.Combine(IApplicationDataService.ApplicationDataPath, Constants.DIR_UPDATES);
            if (!Directory.Exists(updateFolder)) return;
            try
            {
                Directory.Delete(updateFolder, true);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to delete old update assets", ex);
            }
        }

        public async Task ExplicitCheckUpdates()
        {
            //Explicit trigger, clear notified updates to show popup again if found
            preferencesService.Preferences.UpdatePreferences.NotifiedUpdates.Clear();

            await CheckForUpdates();
            if (!_updates.Any())
            {
                Notification not = new("No updates", "No updates were found. You are on the latest version of COMPASS.");
                notificationService.Notify(not);
            }
        }

        private async Task RunUpdateCheckLoop(CancellationToken cancellationToken)
        {
            await CheckForUpdates().ConfigureAwait(false);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await CheckForUpdates().ConfigureAwait(false);
            }
        }

        private async Task CheckForUpdates()
        {
            try
            {
                var updatePrefs = preferencesService.Preferences.UpdatePreferences;
                _updates.Clear();
                var updates = await updateService.CheckForUpdates(updatePrefs.IncludePrerelease).ConfigureAwait(false);
                _updates.AddRange(updates);

                if (!_updates.Any())
                {
                    //No updates so can delete all old update assets
                    ClearUpdateAssets();
                    return;
                }

                OnUpdateFound?.Invoke(this, EventArgs.Empty);
                var latestUpdate = _updates.OrderByDescending(u => u.Version).First();
                await updateService.OnUpdatesFound(_updates);
            }
            catch (Exception ex)
            {
                logger.Error($"Checking for updates failed", ex);
            }
        }

        public static string GetAssetPath(ReleaseAsset asset) 
            => Path.Combine(IApplicationDataService.ApplicationDataPath, Constants.DIR_UPDATES, asset.AssetName);

        public static bool IsChecksumCorrect(byte[] data, string checksum) 
        {
            //Check checksum
            using SHA256 sha256 = SHA256.Create();
            // Compute the hash
            byte[] hashBytes = sha256.ComputeHash(data);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            
            return hashString == checksum;
        }
    }
}