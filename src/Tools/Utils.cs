using COMPASS.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.NetworkInformation;

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

        //put all nodes of a tree in a flat enumerable
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

        //check internet connection
        public static bool pingURL(string URL = "8.8.8.8")
        {
            Ping p = new Ping();
            try
            {
                PingReply reply = p.Send(URL, 3000);
                if (reply.Status == IPStatus.Success)
                    return true;
            }
            catch { }
            return false;
        }

        //Try functions in order determined by list of preferablefunctions untill one succeeds
        public static bool tryFunctions<T>(List<PreferableFunction<T>> toTry, T arg)
        {
            bool success = false;
            int i = 0;
            while (!success && i<toTry.Count)
            {
                success = toTry[i].Function(arg);
                i++;
            }
            return success;
        }
        public static bool tryFunctions<T>(ObservableCollection<PreferableFunction<T>> toTry, T arg)
        {
            return tryFunctions<T>(toTry.ToList(), arg);
        }
    }
}
