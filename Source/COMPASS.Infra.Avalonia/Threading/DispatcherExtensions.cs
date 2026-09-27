using Avalonia.Threading;

namespace COMPASS.Infra.Avalonia.Threading;

public static class DispatcherExtensions
{
    extension(Dispatcher dispatcher)
    {
        public void PostIfNeeded(Action action)
        {
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Post(action);
            }
        }

        public void InvokeIfNeeded(Action action)
        {
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }
    }
}
