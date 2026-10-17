using System.Text.Json;
using System.Text.Json.Nodes;

namespace COMPASS.Infra.Text;

public static class JsonExtensions
{
    extension(JsonNode? node)
    {
        public int? GetIntValue()
        {
            if (node == null) return null;

            var valueKind = node.GetValueKind();
            if (valueKind == JsonValueKind.Number)
            {
                return node.GetValue<int>();
            }
            else if (valueKind == JsonValueKind.String)
            {
                string str = node.GetValue<string>() ?? string.Empty;
                if (int.TryParse(str, out int value))
                {
                    return value;
                }
            }

            return null;
        }


        public string? GetStringValue()
        {
            return node switch
            {
                JsonObject obj => obj["value"]?.GetValue<string>(),
                JsonValue val => val.GetValue<string>(),
                _ => null
            };
        }
    }
}
