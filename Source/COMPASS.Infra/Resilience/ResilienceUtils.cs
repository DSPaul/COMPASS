namespace COMPASS.Infra.Resilience;

public static class ResilienceUtils
{
    public static void Retry(int maxAttempts, Action action, int retryDelayMs = 1000, Action<Exception>? onFailedAttempt = null) =>
        Retry<Exception>(maxAttempts, action, retryDelayMs, onFailedAttempt);
    public static void Retry<T>(int maxAttempts, Action action, int retryDelayMs = 1000, Action<T>? onFailedAttempt = null) where T : Exception
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                action();
                return;
            }
            catch (T e) when (i < maxAttempts - 1) //failure in last attempt will not be caught by design
            {
                onFailedAttempt?.Invoke(e);
                if (retryDelayMs > 0)
                {
                    Thread.Sleep(retryDelayMs);
                }
            }
        }
    }

    public static async Task RetryAsync(int maxAttempts, Func<CancellationToken, Task> action, int retryDelayMs = 1000, Action<Exception>? onFailedAttempt = null, CancellationToken cancellationToken = default) =>
        await RetryAsync<Exception>(maxAttempts, action, retryDelayMs, onFailedAttempt, cancellationToken);
    public static async Task RetryAsync<T>(int maxAttempts, Func<CancellationToken, Task> action, int retryDelayMs = 1000, Action<T>? onFailedAttempt = null, CancellationToken cancellationToken = default) where T : Exception
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await action(cancellationToken);
                return;
            }
            catch(OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Should not be retrying when cancellation is requested, so just rethrow the exception
                throw;
            }
            catch (T e) when (i < maxAttempts - 1) //failure in last attempt will not be caught by design
            {
                onFailedAttempt?.Invoke(e);
                if (retryDelayMs > 0)
                {
                    await Task.Delay(retryDelayMs, cancellationToken);
                }
            }
        }
    }
}
