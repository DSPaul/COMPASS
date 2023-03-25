using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Same as addRange of lists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="range"></param>
        public static void AddRange<T>(
        this ObservableCollection<T> collection, IEnumerable<T> toAdd)
        {
            if (toAdd == null) throw new ArgumentNullException(nameof(toAdd));
            foreach (var i in toAdd) collection.Add(i);
        }

        /// <summary>
        /// Add an object to the end of the list if it is not yet in the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="toAdd"></param>
        /// <returns>Returns true if item was added, false if not </returns>
        public static bool AddIfMissing<T>(
            this IList<T> list, T toAdd)
        {
            if (toAdd == null) throw new ArgumentNullException(nameof(toAdd));
            if (!list.Contains(toAdd))
            {
                list.Add(toAdd);
                return true;
            }
            return false;
        }
        #endregion
    }
}
