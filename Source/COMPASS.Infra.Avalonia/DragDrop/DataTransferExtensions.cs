using Avalonia.Input;
using Avalonia.Media;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace COMPASS.Infra.Avalonia.DragDrop;

public static class DataTransferExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new ColorJsonConverter() }
    };

    extension(DataTransfer transfer)
    {
        public void AddData<T>(DataFormat<T> format, T data) where T : class
        {
            var transferItem = new DataTransferItem();
            transferItem.Set(format, data);
            transfer.Add(transferItem);
        }

        /// <summary>
        /// Serializes data to JSON text before adding it to the transfer.
        /// Legacy workaround from when Avalonia data transfer only supported text;
        /// arbitrary objects can now be added directly via AddData. Kept for cases
        /// where a text representation is explicitly needed.
        /// </summary>
        public void AddJsonData<T>(DataFormat<string> format, T data)
        {
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            var transferItem = new DataTransferItem();
            transferItem.Set(format, json);
            transfer.Add(transferItem);
        }
    }

    extension(IDataTransfer transfer)
    {
        /// <summary>
        /// Deserializes JSON text from the transfer back into an object.
        /// Counterpart to AddJsonData; see its remarks on why this exists.
        /// </summary>
        public T? GetValue<T>(DataFormat<string> format) where T : class
        {
            var json = transfer.TryGetValue(format);
            if (json is null) return null;
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
    }

    private sealed class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Color.FromUInt32(reader.GetUInt32());

        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.ToUInt32());
    }
}
