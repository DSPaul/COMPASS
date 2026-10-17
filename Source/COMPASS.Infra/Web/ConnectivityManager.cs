using COMPASS.Infra.Logging;
using COMPASS.Infra.Progress;
using System.Net;

namespace COMPASS.Infra.Web;

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

                if (UiSynchronizationContext.Current is not null &&
                    UiSynchronizationContext.Current != SynchronizationContext.Current)
                {
                    UiSynchronizationContext.Current.Post(_ => IsOnlineChanged?.Invoke(value), null);
                }
                else
                {
                    IsOnlineChanged?.Invoke(value);
                }
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

    public async Task<bool> VerifyUrlReachable(string url, CancellationToken cancellationToken = default)
    {
        // Any response below 500 means the host is alive (bot walls, auth, 404s all count).
        // Only 5xx or no response at all counts as unreachable so users can still open their link.
        HttpStatusCode? headStatusCode = await TryGetStatusCodeAsync(HttpMethod.Head, url, cancellationToken).ConfigureAwait(false);
        if (headStatusCode.HasValue)
        {
            IsOnline = true;
            if ((int)headStatusCode.Value < 500) return true;
            // HEAD handlers sometimes 500 while GET works, fall through to the GET fallback
        }

        // Fallback: some servers block or mishandle HEAD, but serve GET fine.
        // Range request keeps it lightweight.
        HttpStatusCode? getStatusCode = await TryGetStatusCodeAsync(HttpMethod.Get, url, cancellationToken).ConfigureAwait(false);
        if (getStatusCode.HasValue)
        {
            IsOnline = true;
            return (int)getStatusCode.Value < 500;
        }

        // No response at all: could be us (offline) or them (DNS down).
        // CheckConnection updates IsOnline globally so the UI reflects it.
        await CheckConnection().ConfigureAwait(false);
        return false;
    }

    private async Task<HttpStatusCode?> TryGetStatusCodeAsync(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient(WebService.ConnectionCheckHttpClient);
            using var request = new HttpRequestMessage(method, url);
            if (method == HttpMethod.Get)
            {
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
            }
            using HttpResponseMessage reply = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            return reply.StatusCode;
        }
        catch (HttpRequestException ex) when (ex.StatusCode.HasValue)
        {
            // Some handlers surface non-success statuses via exception; still a live host
            return ex.StatusCode.Value;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is OperationCanceledException // timeout on the 3s connection-check client
                                   || ex is HttpRequestException
                                   || ex is InvalidOperationException
                                   || ex is UriFormatException)
        {
            return null;
        }
    }
}
