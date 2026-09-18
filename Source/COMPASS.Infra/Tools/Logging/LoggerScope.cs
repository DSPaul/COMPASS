namespace COMPASS.Infra.Tools.Logging;

/// <summary>
/// Ambient logger slot for the current async flow. Pipeline roots set it with
/// <see cref="Use"/> so service code that only knows its injected <see cref="ILogger"/>
/// transparently also reports to a progress tracker (via <see cref="ScopedForwardingLogger"/>).
/// The value flows across await, Task.Run and Parallel.ForEachAsync bodies because it rides
/// on AsyncLocal; always pair with using/Dispose so it never leaks into unrelated work.
/// </summary>
public static class LoggerScope
{
    private static readonly AsyncLocal<ILogger?> _current = new();

    /// <summary>
    /// The logger for this async flow, if a scope is active.
    /// </summary>
    public static ILogger? Current => _current.Value;

    /// <summary>
    /// Makes <paramref name="logger"/> the ambient logger until the returned token is disposed.
    /// Tokens nest: disposing restores the previous value.
    /// </summary>
    public static IDisposable Use(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        ILogger? previousLogger = _current.Value;
        _current.Value = logger;
        return new LoggerScopeToken(previousLogger);
    }

    private sealed class LoggerScopeToken(ILogger? previousLogger) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _current.Value = previousLogger;
            GC.SuppressFinalize(this);
        }
    }
}
