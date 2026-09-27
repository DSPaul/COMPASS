using System.Diagnostics.CodeAnalysis;

namespace COMPASS.Infra.Collections;

public static class CollectionExtensions
{
    extension<T>(ICollection<T> collection)
    {
        /// <summary>
        /// Add an object to the end of the list if it is not yet in the list.
        /// </summary>
        /// <param name="toAdd"></param>
        /// <returns>Returns true if item was added, false if not </returns>
        public bool AddIfMissing(T toAdd)
        {
            if (toAdd == null) throw new ArgumentNullException(nameof(toAdd));
            if (!collection.Contains(toAdd))
            {
                collection.Add(toAdd);
                return true;
            }
            return false;
        }
    }

    extension<T>(IEnumerable<T> items) where T : IHasChildren<T>
    {
        public IEnumerable<T> Flatten(string method = "dfs")
            => items.Flatten(item => item.Children, method);
    }

    extension<T>(IEnumerable<T> items)
    {
        public IEnumerable<T> Flatten(Func<T, IEnumerable<T>> childSelector, string method = "dfs")
        {
            var result = items.ToList();

            switch (method)
            {
                //Breadth first search
                case "bfs":
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            T parent = result[i];
                            result.AddRange(childSelector(parent));
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
                            result.InsertRange(i + 1, childSelector(parent));
                            yield return parent;
                        }
                        break;
                    }
            }
        }

        public IEnumerable<T> Without(T element)
        {
            return items.Except([element]);
        }

        public bool HasCommonValue<TKey>(Func<T, TKey> keySelector, out TKey? value)
        {
            value = default;
            if (!items.Any())
            {
                return false;
            }

            value = keySelector(items.First());
            TKey key = value;
            return items.Skip(1).All(item => EqualityComparer<TKey>.Default.Equals(keySelector(item), key));
        }
    }

    extension<T>(IEnumerable<T?> items)
    {
        public IEnumerable<T> RemoveNulls()
        {
            return items.Where(item => item is not null).Cast<T>();
        }
    }

    extension<T>([NotNullWhen(true)] IEnumerable<T>? items)
    {
        public bool SafeAny()
            => items != null && items.Any();
    }
}
