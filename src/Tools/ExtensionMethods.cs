using COMPASS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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

        public static bool SafeAny<T>([NotNullWhen(true)] this IEnumerable<T>? l) => l is not null && l.Any();
        #endregion
    }
}
