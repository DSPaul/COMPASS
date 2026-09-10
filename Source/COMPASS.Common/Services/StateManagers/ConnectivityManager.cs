using Avalonia.Controls;
using Avalonia.Threading;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services;

namespace COMPASS.Common.Services.StateManagers;

public class ConnectivityManager(
    ILogger logger,
    IHttpClientFactory httpClientFactory)
{
    private static readonly TimeSpan _connectionCheckCooldown = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _connectionCheckInterval = TimeSpan.FromMinutes(5);
    private DateTime _lastConnectionCheck = DateTime.MinValue;

    private readonly CancellationTokenSource _cts = new();
    private bool _timerPaused = false;
    private bool _fireOnResume = false;

    public event Action<bool>? IsOnlineChanged;

    public bool IsOnline
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                if (!value)
                {
                    logger.Warn("COMPASS is offline, some features might not work.");
                }
                else
                {
                    logger.Info("Internet connection restored");
                }
                Dispatcher.UIThread.Post(() => IsOnlineChanged?.Invoke(value));
            }
        }
    } = true;

    public void Start()
    {
        _ = RunAsync(_cts.Token);
        _ = CheckConnection();
    }

    public void SubscribeToWindowFocus(Window window)
    {
        window.Activated += (_, _) => Resume();
        window.Deactivated += (_, _) => Pause();
    }

    public void Pause() => _timerPaused = true;

    public void Resume()
    {
        _timerPaused = false;
        if (_fireOnResume)
        {
            _fireOnResume = false;
            _ = CheckConnection();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
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

    public async Task<bool> CheckConnection()
    {
        // Cooldown so we don't check unreasonably often whether or not there is an internet connection.
        if (DateTime.UtcNow - _lastConnectionCheck < _connectionCheckCooldown)
            return IsOnline;

        try
        {
            _lastConnectionCheck = DateTime.UtcNow;
            var client = httpClientFactory.CreateClient(WebService.ConnectionCheckHttpClient);
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

    public async Task<bool> VerifyUrlReachable(string url)
    {
        try
        {
            var client = httpClientFactory.CreateClient(WebService.ConnectionCheckHttpClient);
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
