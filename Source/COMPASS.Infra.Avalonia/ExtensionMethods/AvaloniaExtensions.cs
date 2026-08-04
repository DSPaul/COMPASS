using Avalonia.Collections;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace COMPASS.Infra.Avalonia.ExtensionMethods
{
    public static class AvaloniaExtensions
    {
        public static T? FindChild<T>(this ILogical visual, Func<T, bool> predicate)
        {
            if (visual is T t && predicate(t))
            {
                return t;
            }

            foreach (var child in visual.GetLogicalChildren())
            {
                var result = child.FindChild(predicate);
                if (result != null) return result;
            }

            return default;
        }

        /// <summary>
        /// Sort an AvaloniaList in place
        /// </summary>
        /// <param name="keySelector"></param>
        /// <param name="sortDirection"></param>
        /// <typeparam name="TKey"></typeparam>
        public static void Sort<T, TKey>(this AvaloniaList<T> list, Func<T, TKey> keySelector, ListSortDirection sortDirection = ListSortDirection.Ascending)
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

        public static void PostIfNeeded(this Dispatcher dispatcher, Action action)
        {
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Post(action);
            }
        }

        #region Json Serialization for DataTransfer

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new ColorJsonConverter() }
        };

        public static void AddData<T>(this DataTransfer transfer, DataFormat<T> format, T data) where T : class
        {
            var transferItem = new DataTransferItem();
            transferItem.Set(format, data);
            transfer.Add(transferItem);
        }

        public static void AddJsonData<T>(this DataTransfer transfer, DataFormat<string> format, T data)
        {
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            var transferItem = new DataTransferItem();
            transferItem.Set(format, json);
            transfer.Add(transferItem);
        }

        public static T? GetValue<T>(this IDataTransfer transfer, DataFormat<string> format) where T : class
        {
            var json = transfer.TryGetValue(format);
            if (json is null) return null;
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        #endregion

        private sealed class ColorJsonConverter : JsonConverter<Color>
        {
            public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => Color.FromUInt32(reader.GetUInt32());

            public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value.ToUInt32());
        }
    }
}
