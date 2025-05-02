using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using COMPASS.Common.Models;

namespace COMPASS.Common.Tools
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

        public static void Shutdown()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
            {
                desktopApp.Shutdown();
            }
        }
    }
}
