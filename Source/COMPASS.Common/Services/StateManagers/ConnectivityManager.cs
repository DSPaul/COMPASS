using Avalonia.Controls;
using Avalonia.Threading;
using COMPASS.Infra.Tools.Logging;

namespace COMPASS.Common.Services.StateManagers;

public class ConnectivityManager(
    ILogger logger,
    IHttpClientFactory httpClientFactory) : IDisposable
{
    private static readonly TimeSpan _connectionCheckCooldown = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _connectionCheckInterval = TimeSpan.FromMinutes(5);
    private DateTime _lastConnectionCheck = DateTime.MinValue;

    private readonly object _lifecycleLock = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _disposed;
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
        lock (_lifecycleLock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ConnectivityManager));
            if (_loopTask is not null) return;

            _cts = new CancellationTokenSource();
            _loopTask = RunAsync(_cts.Token);
        }
        _ = CheckConnection();
    }

    public async Task StopAsync()
    {
        Task? loopTask;
        CancellationTokenSource? cts;
        lock (_lifecycleLock)
        {
            loopTask = _loopTask;
            cts = _cts;
            _loopTask = null;
            _cts = null;
        }

        if (loopTask is null) return;

        cts?.Cancel();
        try
        {
            await loopTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            //Expected on shutdown; RunAsync also guards so nothing escapes unobserved
        }
        cts?.Dispose();
    }

    public void Dispose()
    {
        lock (_lifecycleLock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        //Synchronous shutdown via StopAsync.
        //Blocking here is deadlock-free: every await below uses ConfigureAwait(false)
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            //Never throw from Dispose
        }
        GC.SuppressFinalize(this);
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
        //Swallows cancellation: this loop is fire-and-forget
        //so any exception will cause crash, such as cancelledException
        try
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
        catch (OperationCanceledException)
        {
            
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
