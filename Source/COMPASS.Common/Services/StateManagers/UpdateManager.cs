using COMPASS.Common.Interfaces.Services;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Services.StateManagers
{
    public static class UpdateManager
    {
        private static ILogger _logger = ServiceResolver.Resolve<ILogger>();
        private static IUpdateService? _updateService;
        private static readonly CancellationTokenSource _cts = new();

        public static void Run(IUpdateService service)
        {
            _updateService = service;
            _ = RunAsync(_cts.Token);
        }

        private static async Task RunAsync(CancellationToken cancellationToken)
        {
            await CheckForUpdates().ConfigureAwait(false);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await CheckForUpdates().ConfigureAwait(false);
            }
        }

        public static async Task CheckForUpdates()
        {
            if (_updateService is null)
            {
                throw new InvalidOperationException("UpdateManager not initialized with an IUpdateService.");
            }

            try
            {
                var updates = await _updateService.CheckForUpdates().ConfigureAwait(false);
                await _updateService.HandleUpdates(updates);
            }
            catch (Exception ex)
            {
                _logger.Error($"Checking for updates failed", ex);
            }
        }
    }
}