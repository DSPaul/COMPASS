using COMPASS.Infra.Models.Progress;
using COMPASS.Infra.Tools.Logging;

namespace COMPASS.Common.Services.StateManagers;

/// <summary>
/// Central registry of in-progress operations. This is deliberately NOT a scheduler:
/// work runs as plain async/await at the call site; the registry only tracks trackers
/// (for the progress UI) and owns the cancellation tokens (for cancel-all).
/// </summary>
public sealed class ProgressTrackingManager : IDisposable
{
    private readonly object managerLock = new();
    private readonly List<TrackedOperation> _trackedOperations = [];
    private CancellationTokenSource _globalCancellation = new();
    private bool _disposed;

    /// <summary>
    /// Raised whenever an task is tracked or untracked.
    /// </summary>
    public event EventHandler? TrackingChanged;

    /// <summary>
    /// Run some work with an attached tracker, provides a cancellationToken
    /// </summary>
    public async Task RunAsync(ProgressTracker tracker, string title, Func<ProgressTracker, CancellationToken, Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        TrackedOperation operation = Track(tracker, title);
        try
        {
            using (LoggerScope.Use(tracker))
            await work(tracker, operation.CancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Untrack(operation);
        }
    }

    /// <summary>
    /// Registers a tracker and returns its operation (title, token, cancel).
    /// Untrack + dispose it when the work is done, preferably via <see cref="RunAsync"/>.
    /// </summary>
    public TrackedOperation Track(ProgressTracker tracker, string title)
    {
        TrackedOperation operation;
        lock (managerLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            operation = new TrackedOperation(tracker, title,
                CancellationTokenSource.CreateLinkedTokenSource(_globalCancellation.Token));
            _trackedOperations.Add(operation);
        }
        TrackingChanged?.Invoke(this, EventArgs.Empty);
        return operation;
    }

    public void Untrack(TrackedOperation operation)
    {
        lock (managerLock)
        {
            _trackedOperations.Remove(operation);
            operation.Dispose();
        }
        TrackingChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Point-in-time copy of all tracked operations.
    /// </summary>
    public IReadOnlyList<TrackedOperation> GetSnapshot()
    {
        lock (managerLock)
        {
            return [.. _trackedOperations];
        }
    }

    public void CancelAll()
    {
        lock (managerLock)
        {
            if (_disposed) return;
            _globalCancellation.Cancel();
            _globalCancellation.Dispose();
            _globalCancellation = new CancellationTokenSource();
        }
    }

    public void Dispose()
    {
        lock (managerLock)
        {
            if (_disposed) return;
            _disposed = true;
            _globalCancellation.Cancel();
            _globalCancellation.Dispose();
            foreach (TrackedOperation operation in _trackedOperations)
            {
                operation.Dispose();
            }
            _trackedOperations.Clear();
        }
        GC.SuppressFinalize(this);
    }
}

public sealed class TrackedOperation : IDisposable
{
    private readonly CancellationTokenSource _cancellation;
    private bool _disposed;

    internal TrackedOperation(ProgressTracker tracker, string title, CancellationTokenSource cancellation)
    {
        Tracker = tracker;
        Title = title;
        _cancellation = cancellation;
    }

    public ProgressTracker Tracker { get; }
    public string Title { get; }
    public CancellationToken CancellationToken => _cancellation.Token;

    public void Cancel()
    {
        try
        {
            _cancellation.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already untracked and disposed; nothing left to cancel.
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cancellation.Dispose();
        GC.SuppressFinalize(this);
    }
}
