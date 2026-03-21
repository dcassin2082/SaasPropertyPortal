using System.Text.Json;
using System.Text.Json.Serialization;

namespace PropertyPortal.API.Converters
{
    public class ByteArrayToNullableBase64Converter : JsonConverter<byte[]>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;

            var base64String = reader.GetString();
            return string.IsNullOrEmpty(base64String) ? null : Convert.FromBase64String(base64String);
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(Convert.ToBase64String(value));
        }
    }
}
