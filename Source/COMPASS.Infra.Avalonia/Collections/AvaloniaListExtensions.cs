using Avalonia.Collections;
using System.ComponentModel;

namespace COMPASS.Infra.Avalonia.Collections;

public static class AvaloniaListExtensions
{
    extension<T>(AvaloniaList<T> list)
    {
        /// <summary>
        /// Sort an AvaloniaList in place
        /// </summary>
        /// <param name="keySelector"></param>
        /// <param name="sortDirection"></param>
        /// <typeparam name="TKey"></typeparam>
        public void Sort<TKey>(Func<T, TKey> keySelector, ListSortDirection sortDirection = ListSortDirection.Ascending)
        {
            List<T> sorted = sortDirection switch
            {
                ListSortDirection.Ascending => list.OrderBy(keySelector).ToList(),
                ListSortDirection.Descending => list.OrderByDescending(keySelector).ToList(),
                _ => throw new ArgumentOutOfRangeException(nameof(sortDirection), sortDirection, null)
            };

            for (int i = 0; i < sorted.Count(); i++)
            {
                var prevIdx = list.IndexOf(sorted[i]);
                if (prevIdx != i)
                {
                    list.Move(prevIdx, i);
                }
            }
        }
    }
}
