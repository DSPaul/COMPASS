using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace COMPASS.Tools
{
    public static class Utils
    {
        public static int GetAvailableID<T>(IEnumerable<T> collection) where T : IHasID
        {
            int tempID = 0;
            IList<int> usedIDs = collection.Select(x => x.ID).ToList();
            while (usedIDs.Contains(tempID))
            {
                tempID++;
            }
            return tempID;
        }

        public static void Retry(int maxAttempts, Action action, int retryDelayMs = 1000, Action<Exception>? onFailedAttempt = null) =>
            Retry<Exception>(maxAttempts, action, retryDelayMs, onFailedAttempt);
        public static void Retry<T>(int maxAttempts, Action action, int retryDelayMs = 1000, Action<Exception>? onFailedAttempt = null) where T : Exception
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

    }
}
