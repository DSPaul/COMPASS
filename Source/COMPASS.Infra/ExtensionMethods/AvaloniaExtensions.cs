using Avalonia;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace COMPASS.Infra.ExtensionMethods
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

        #region Drag & Drop

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new ColorJsonConverter() }
        };

        public static void AddData<T>(this DataTransfer transfer, DataFormat<string> format, T data)
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
