using COMPASS.Infra.ExtensionMethods;

namespace COMPASS.Common.Tools;

/// <summary>
/// A class for batching/deferring events within a scope.
/// Events triggered during the scope are batched and fired once when the scope exits.
/// </summary>
public class EventDeferralScope<T> where  T : EventArgs
{
    private bool _deferEvents = false;
    private readonly Action<T> _notifyAction;
    private readonly IList<T> _eventArgs = [];

    /// <summary>
    /// Creates a new EventDeferralScope with the specified notification action.
    /// </summary>
    /// <param name="notifyAction">The action to invoke when events should be fired</param>
    public EventDeferralScope(Action<T> notifyAction)
    {
        _notifyAction = notifyAction;
    }

    /// <summary>
    /// Call this method wherever you would normally fire the event.
    /// During a deferral scope, this schedules the event instead of firing it immediately.
    /// </summary>
    /// <param name="eventArgs"></param>
    public void Notify(T eventArgs)
    {
        if (_deferEvents)
        {
            _eventArgs.AddIfMissing(eventArgs);
            return;
        }
        
        _notifyAction(eventArgs);
    }

    /// <summary>
    /// Begins a deferral scope. Multiple calls can be nested; only the outermost scope fires events.
    /// </summary>
    /// <returns>A disposable scope that fires deferred events when disposed</returns>
    public DeferralScope BeginDeferral() => new DeferralScope(this);

    public class DeferralScope : IDisposable
    {
        private readonly EventDeferralScope<T> _parent;
        private readonly bool _active;

        public DeferralScope(EventDeferralScope<T> parent)
        {
            _parent = parent;
            
            if (_parent._deferEvents)
            {
                // Already a deferral scope present, outer scope always wins
                _active = false;
            }
            else
            {
                _parent._deferEvents = true;
                _active = true;
            }
        }

        public void Dispose()
        {
            if (!_active) return;
            
            _parent._deferEvents = false;
            if (_parent._eventArgs.Any())
            {
                foreach (var arg in _parent._eventArgs.Distinct())
                {
                    _parent.Notify(arg);
                }
                _parent._eventArgs.Clear();
            }
        }
    }
}

public class EventDeferralScope : EventDeferralScope<EventArgs>
{
    /// <summary>
    /// Creates a new EventDeferralScope with the specified notification action.
    /// </summary>
    /// <param name="notifyAction">The action to invoke when events should be fired</param>
    public EventDeferralScope(Action<EventArgs> notifyAction) : base(notifyAction)
    {
        
    }

    public void Notify()
    {
        Notify(EventArgs.Empty);
    }
}