using COMPASS.Infra.Models.Interfaces;

namespace COMPASS.Infra.Tools
{
    public static class Utils
    {
        public static int GetAvailableId<T>(IEnumerable<T> collection) where T : IHasId
        {
            int tempId = 0;
            IList<int> usedIds = collection.Select(x => x.Id).ToList();
            while (usedIds.Contains(tempId))
            {
                tempId++;
            }
            return tempId;
        }

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

        public static async Task RetryAsync(int maxAttempts, Func<Task> action, int retryDelayMs = 1000, Action<Exception>? onFailedAttempt = null) =>
            await RetryAsync<Exception>(maxAttempts, action, retryDelayMs, onFailedAttempt);
        public static async Task RetryAsync<T>(int maxAttempts, Func<Task> action, int retryDelayMs = 1000, Action<T>? onFailedAttempt = null) where T : Exception
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    await action();
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
    }
}
