using Avalonia.Controls;
using Avalonia.Threading;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Services.StateManagers;

public static class ConnectivityManager
{
    private static ILogger Logger => ServiceResolver.Resolve<ILogger>();
    private static IHttpClientFactory HttpClientFactory => ServiceResolver.Resolve<IHttpClientFactory>();

    private static readonly TimeSpan _connectionCheckCooldown = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _connectionCheckInterval = TimeSpan.FromMinutes(5);
    private static DateTime _lastConnectionCheck = DateTime.MinValue;

    private static readonly CancellationTokenSource _cts = new();
    private static bool _timerPaused = false;
    private static bool _fireOnResume = false;

    public static event Action<bool>? IsOnlineChanged;

    public static bool IsOnline
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                if (!value)
                {
                    Logger.Warn("COMPASS is offline, some features might not work.");
                }
                else
                {
                    Logger.Info("Internet connection restored");
                }
                Dispatcher.UIThread.Post(() => IsOnlineChanged?.Invoke(value));
            }
        }
    } = true;

    public static void Start()
    {
        _ = RunAsync(_cts.Token);
        _ = CheckConnection();
    }

    public static void SubscribeToWindowFocus(Window window)
    {
        window.Activated += (_, _) => Resume();
        window.Deactivated += (_, _) => Pause();
    }

    public static void Pause() => _timerPaused = true;

    public static void Resume()
    {
        _timerPaused = false;
        if (_fireOnResume)
        {
            _fireOnResume = false;
            _ = CheckConnection();
        }
    }

    private static async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_connectionCheckInterval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!_timerPaused)
            {
                await CheckConnection().ConfigureAwait(false);
            }
            else
            {
                _fireOnResume = true;
            }
        }
    }

    public static async Task<bool> CheckConnection()
    {
        // Cooldown so we don't check unreasonably often whether or not there is an internet connection.
        if (DateTime.UtcNow - _lastConnectionCheck < _connectionCheckCooldown)
            return IsOnline;

        try
        {
            _lastConnectionCheck = DateTime.UtcNow;
            var client = HttpClientFactory.CreateClient(WebService.ConnectionCheckHttpClient);
            using HttpResponseMessage reply = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "https://google.com")).ConfigureAwait(false);
            reply.EnsureSuccessStatusCode();
            IsOnline = true;
        }
        catch (Exception)
        {
            IsOnline = false;
        }

        return IsOnline;
    }

    public static async Task<bool> VerifyUrlReachable(string url)
    {
        try
        {
            var client = HttpClientFactory.CreateClient(WebService.ConnectionCheckHttpClient);
            using HttpResponseMessage reply = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url)).ConfigureAwait(false);
            reply.EnsureSuccessStatusCode();
            IsOnline = true;
            return true;
        }
        catch (HttpRequestException)
        {
            await CheckConnection().ConfigureAwait(false);
            return false;
        }
    }
}
