using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.Json;

public sealed class DateTimeOffsetConv : JsonConverter<DateTimeOffset> {
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        DateTimeOffset dt = reader.TokenType switch {
            JsonTokenType.Null or JsonTokenType.None => default,
            JsonTokenType.String or JsonTokenType.PropertyName => Parse(reader.GetString()),
            JsonTokenType.Number => DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64()),
            _ => ThrowJsonTokenInvalid()
        };

        return dt;
    }

    public override DateTimeOffset ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) {
        writer.WriteStringValue(ToString(in value));
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) {
        writer.WritePropertyName(ToString(in value));
    }

    public static string ToString(in DateTimeOffset dt) {
        return DateTimeConv.ToStringUtc(dt.UtcDateTime);
    }
    
    public static DateTimeOffset Parse(in ReadOnlySpan<char> str) {
        return DateTimeConv.Parse(in str);
    }
    
    [DoesNotReturn]
    private static DateTimeOffset ThrowJsonTokenInvalid() {
        throw new JsonException("Cannot deserialize a non numeric non string token as a DateTime.");
    }
}