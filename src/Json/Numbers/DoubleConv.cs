using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.Json.Numbers;

public sealed class DoubleConv : JsonConverter<double> {
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is JsonTokenType.Null or JsonTokenType.None) {
            return default;
        }

        if (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName) {
            var str = reader.GetString();
            if (String.IsNullOrEmpty(str)) {
                return default;
            }

            double v = SpecialNumbers.ToDouble(str);
            if (v != default) {
                return v;
            }

            return Double.Parse(str, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
        }

        if (reader.TokenType == JsonTokenType.Number) {
            return reader.GetDouble();
        }

        throw new JsonException("Could not parse the number.");
    }

    public override double ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) {
        string? str = SpecialNumbers.ToSpecial(in value);
        if (str is not null) {
            writer.WriteStringValue(str.AsSpan());
            return;
        }

        writer.WriteNumberValue(value);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, double value, JsonSerializerOptions options) {
        string? str = SpecialNumbers.ToSpecial(in value);
        if (str is not null) {
            writer.WritePropertyName(str.AsSpan());
            return;
        }

        writer.WritePropertyName(value.ToString(null, NumberFormatInfo.InvariantInfo).AsSpan());
    }
}