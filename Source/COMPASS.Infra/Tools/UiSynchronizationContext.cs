namespace COMPASS.Infra.Tools;

/// <summary>
/// App-wide UI <see cref="SynchronizationContext"/>, registered once at startup.
/// <see cref="ProgressTracker"/> and other background-reporting code marshal through here,
/// so they work no matter which thread constructs or reports to them.
/// Falls back to the ambient context when nothing is registered (e.g. unit tests,
/// where synchronous inline notification is what callers expect).
/// </summary>
public static class UiSynchronizationContext
{
    private static SynchronizationContext? _registeredContext;

    public static SynchronizationContext? Current => _registeredContext ?? SynchronizationContext.Current;

    /// <summary>
    /// Registers the UI context. Call once, from the UI thread, during startup.
    /// </summary>
    public static void Initialize(SynchronizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _registeredContext = context;
    }
}
