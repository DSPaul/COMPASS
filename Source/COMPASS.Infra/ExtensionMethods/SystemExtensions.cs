using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Interfaces;
using FuzzySharp;

namespace COMPASS.Infra.ExtensionMethods
{
    public static class SystemExtensions
    {
        #region ObservableCollection Extensions
       
        
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
            return RegexConstants.Numbers().Replace(input, match => match.Value.PadLeft(totalWidth, '0'));
        }

        public static string RemoveDiacritics(this string text) =>
            //"héllo" becomes "he<acute>llo", which in turn becomes "hello".
            string.Concat(text.Normalize(NormalizationForm.FormD).Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)).Normalize(NormalizationForm.FormC);

        public static bool MatchesFuzzy(this string? input, string pattern, int threshold = 80)
        {
            if (string.IsNullOrEmpty(pattern)) return true; //an empty pattern matches everything
            if (string.IsNullOrEmpty(input)) return false;
            
            string loweredInput = input.ToLower();
            string loweredPattern = pattern.ToLower();
            return Fuzz.TokenInitialismRatio(loweredInput, loweredPattern) > threshold || //include acronyms
                    input.Contains(pattern, StringComparison.CurrentCultureIgnoreCase) || //include string fragments
                    Fuzz.PartialRatio(loweredInput, loweredPattern) > 80; //include spelling errors
        }
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

        public static List<string> GetObsoleteProperties(this Type type)
        {
            List<string> obsoleteProperties = [];

            // Check each property for the presence of the Obsolete attribute
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (Attribute.GetCustomAttribute(property, typeof(ObsoleteAttribute)) is ObsoleteAttribute)
                {
                    obsoleteProperties.Add(property.Name);
                }
            }

            return obsoleteProperties;
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

        public static IEnumerable<T> Without<T>(this IEnumerable<T> l, T element)
        {
            return l.Except([element]);
        }

        public static IEnumerable<T> RemoveNulls<T>(this IEnumerable<T?> l)
        {
            return l.Where(item => item is not null).Cast<T>();
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
            return l.Skip(1).All(item => EqualityComparer<TKey>.Default.Equals(keySelector(item), key));
        }

        #endregion

        #region Json Extensions

        public static int? GetIntValue(this JsonNode? node)
        {
            if (node == null) return null;

            try
            {
                return node.GetValue<int>();
            }
            catch(FormatException)
            {
                string? str = node.GetValue<string>();
                if(int.TryParse(str, out int value))
                {
                    return value;
                }
            }

            return null;
        }

        #endregion
    }
}
