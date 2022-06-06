using COMPASS.Models;
using System.Collections.Generic;
using System.Linq;

namespace COMPASS.Tools
{
    public static class Utils
    {
        public static int GetAvailableID<T>(List<T> Collection) where T : IHasID
        {
            int tempID = 0;
            while (Collection.Any(f => f.ID == tempID))
            {
                tempID++;
            }
            return tempID;
        }

        public static IEnumerable<T> FlattenTree<T>(IEnumerable<T> l) where T: IHasChilderen<T>
        {
            var result = new List<T>(l);
            for (int i = 0; i < result.Count(); i++)
            {
                T parent = result[i];
                foreach (T child in parent.Children) result.Add(child);
            }
            return result;
        }

    }
}
