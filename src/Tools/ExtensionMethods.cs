using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace COMPASS.Tools
{
    public static class ExtensionMethods
    {
        #region ObservableCollection Extensions
        /// <summary>
        /// Remove All Items in place that match the condition.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="condition"></param>
        public static void RemoveAll<T>(
        this ObservableCollection<T> collection, Func<T, bool> condition)
        {
            var itemsToRemove = collection.Where(condition);
            if (itemsToRemove.Count() == collection.Count)
            {
                collection.Clear();
            }

            else
            {
                foreach (var itemToRemove in itemsToRemove)
                {
                    collection.Remove(itemToRemove);
                }
            }
        }
        #endregion
    }
}
