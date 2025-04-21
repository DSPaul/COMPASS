using Avalonia.Input;
using COMPASS.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Org.BouncyCastle.Tls;

namespace COMPASS.Common.Tools
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
        public static void RemoveWhere<T>(
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
        /// Sort an obervable collection in place
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="keySelector"></param>
        /// <param name="sortDirection"></param>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        public static void Sort<TSource,TKey>(this ObservableCollection<TSource> collection, Func<TSource, TKey> keySelector, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            List<TSource> sorted = sortDirection switch
            {
                ListSortDirection.Ascending => collection.OrderBy(keySelector).ToList(),
                ListSortDirection.Descending => collection.OrderByDescending(keySelector).ToList(),
                _ => throw new ArgumentOutOfRangeException(nameof(sortDirection), sortDirection, null)
            };

            for (int i = 0; i < sorted.Count(); i++)
            {
                collection.Move(collection.IndexOf(sorted[i]), i);
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
        private static PropertyInfo? GetPropertyInfo(this object obj, string propName)
        {
            if (obj is null) throw new ArgumentNullException(nameof(obj));
            if (String.IsNullOrWhiteSpace(propName)) throw new ArgumentNullException(nameof(propName), propName);

            Type? type = obj.GetType();

            //keep going up the inheritance tree to find it
            while (type is not null)
            {
                try
                {
                    PropertyInfo? info = type.GetProperty(propName);
                    if (info is not null) return info;
                }
                catch (AmbiguousMatchException) { }

                type = type.BaseType;
            }
            return null;
        }
        public static object? GetPropertyValue(this object obj, string propName)
        {
            var propInfo = obj.GetPropertyInfo(propName) ?? throw new MissingFieldException(propName);
            return propInfo.GetValue(obj);
        }

        public static object? GetDeepPropertyValue(this object obj, string fullPropName)
        {
            if (String.IsNullOrEmpty(fullPropName))
            {
                return obj;
            }

            string[] propNames = fullPropName.Split('.');
            object? result = obj;
            foreach (string propName in propNames)
            {
                result = result?.GetPropertyValue(propName);
            }
            return result;
        }

        public static void SetProperty(this object obj, string propName, object? value)
        {
            var propInfo = obj.GetPropertyInfo(propName);

            if (propInfo is not null && propInfo.CanWrite)
            {
                propInfo.SetValue(obj, value);
            }
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

        public static bool SafeAny<T>(
            [NotNullWhen(true)] this IEnumerable<T>? l) 
            => l != null && l.Any();

        public static bool HasCommonValue<T, TKey>(this IEnumerable<T>? l, Func<T, TKey> keySelector, out TKey? value)
        {
            value = default;
            if (!l.SafeAny())
            {
                return false;
            }

            value = keySelector(l.First());
            TKey key = value;
            return l.All(item => EqualityComparer<TKey>.Default.Equals(keySelector(item), key));
        }
        #endregion

        #region Drag & Drop

        /// <summary>
        /// Tries to get an object of a certain type from the data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns>null if not found</returns>
        public static T? GetValue<T>(this IDataObject data) where T : class
        {
            string format = typeof(T).Name;

            if (data.Contains(format))
            {
                return data.Get(typeof(T).Name) as T;
            }
            else return null;
        }

        #endregion
    }
}
