using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

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
            var itemsToRemove = collection.Where(condition).ToList();
            if (itemsToRemove.Count == collection.Count)
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
        /// <param name="toAdd"></param>
        public static void AddRange<T>(
        this ObservableCollection<T> collection, IEnumerable<T> toAdd)
        {
            if (toAdd == null) throw new ArgumentNullException(nameof(toAdd));
            foreach (var i in toAdd)
            {
                collection.Add(i);
            }
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

        #region String Extensions
        public static string PadNumbers(this string input, int totalWidth = 8)
        {
            if (String.IsNullOrEmpty(input)) return input;
            return Constants.RegexNumbersOnly().Replace(input, match => match.Value.PadLeft(totalWidth, '0'));
        }

        public static string RemoveDiacritics(this string text) =>
            //"héllo" becomes "he<acute>llo", which in turn becomes "hello".
            string.Concat(text.Normalize(NormalizationForm.FormD).Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)).Normalize(NormalizationForm.FormC);
        #endregion

        #region ReflectionExtentions
        //From BlackPearl
        public static object GetPropertyValue(this object obj, string path)
        {
            if (string.IsNullOrEmpty(path) || obj == null)
            {
                return obj;
            }

            int dotIndex = path.IndexOf('.');
            if (dotIndex < 0)
            {
                return GetValue(obj, path);
            }

            obj = GetValue(obj, path.Substring(0, dotIndex + 1));
            path = path.Remove(0, dotIndex);

            return obj.GetPropertyValue(path);
        }

        //From BlackPearl
        private static object GetValue(object obj, string propertyName)
        {
            PropertyInfo propInfo = obj.GetType().GetProperty(propertyName);
            if (propInfo == null)
            {
                return null;
            }

            return propInfo.GetValue(obj);
        }
        #endregion

        #region EnumerableExtensions
        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> l, string method = "dfs") where T : IHasChildren<T>
        {
            var result = l.ToList();

            switch (method)
            {
                //Breadth first search
                case "bfs":
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            T parent = result[i];
                            result.AddRange(parent.Children);
                            yield return parent;
                        }
                        break;
                    }
                //Depth first search (pre-order)
                case "dfs":
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            T parent = result[i];
                            result.InsertRange(i + 1, parent.Children);
                            yield return parent;
                        }
                        break;
                    }
            }
        }
        #endregion
    }
}
