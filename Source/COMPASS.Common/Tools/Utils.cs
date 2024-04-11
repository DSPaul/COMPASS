using COMPASS.Models;
using System.Collections.Generic;
using System.Linq;

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
    }
}
