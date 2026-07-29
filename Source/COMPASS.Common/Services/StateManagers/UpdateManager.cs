using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models.Updates;
using System.Security.Cryptography;

namespace COMPASS.Common.Services.StateManagers
{
    public class UpdateManager(
        ILogger logger,
        IUpdateService updateService,
        IPreferencesService preferencesService)
    {
        private readonly CancellationTokenSource _cts = new();

        private static readonly List<Update> _updates = [];

        public void StartUpdateCheckLoop()
        {
            if (preferencesService.Preferences.UpdatePreferences.CheckForUpdates) 
            { 
                _ = RunUpdateCheckLoop(_cts.Token);
            }
        }

        public async Task ExplicitCheckUpdates()
        {
            //Explicit trigger, clear skipped updates to give user another chance
            preferencesService.Preferences.UpdatePreferences.SkippedUpdates.Clear();

            await CheckForUpdates();
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
                _updates.AddRange(updates.Where(u => !updatePrefs.SkippedUpdates.Contains(u.Version.ToString())));

                if (!_updates.Any())
                {
                    return;
                }

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