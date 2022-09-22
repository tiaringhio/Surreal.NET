using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.Json.Numbers;

public sealed class SingleConv : JsonConverter<float> {
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is JsonTokenType.Null or JsonTokenType.None) {
            return default;
        }

        if (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName) {
            var str = reader.GetString();
            if (String.IsNullOrEmpty(str)) {
                return default;
            }

            float v = SpecialNumbers.ToSingle(str);
            if (v != default) {
                return v;
            }

            return Single.Parse(str, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
        }

        if (reader.TokenType == JsonTokenType.Number) {
            return reader.GetSingle();
        }

        throw new JsonException("Could not parse the number.");
    }

    public override float ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options) {
        string? str = SpecialNumbers.ToSpecial(in value);
        if (str is not null) {
            writer.WriteStringValue(str.AsSpan());
            return;
        }

        writer.WriteNumberValue(value);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, float value, JsonSerializerOptions options) {
        string? str = SpecialNumbers.ToSpecial(in value);
        if (str is not null) {
            writer.WritePropertyName(str.AsSpan());
            return;
        }

        writer.WritePropertyName(value.ToString(null, NumberFormatInfo.InvariantInfo).AsSpan());
    }
}