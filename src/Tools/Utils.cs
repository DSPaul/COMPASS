using COMPASS.Models;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.Tools
{
    public static class Utils
    {
        public static int GetAvailableID(List<IHasID> Collection)
        {
            int tempID = 0;
            while (Collection.Any(f => f.ID == tempID))
            {
                tempID++;
            }
            return tempID;
        }

    }
}
