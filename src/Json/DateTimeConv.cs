using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

using Rustic;

namespace SurrealDB.Json;

public sealed class DateTimeConv : JsonConverter<DateTime> {
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        DateTime dt = reader.TokenType switch {
            JsonTokenType.Null or JsonTokenType.None => default,
            JsonTokenType.String or JsonTokenType.PropertyName => Parse(reader.GetString()),
            JsonTokenType.Number => DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64()).UtcDateTime,
            _ => ThrowJsonTokenTypeInvalid()
        };

        return dt;
    }

    public override DateTime ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
        writer.WriteStringValue(ToString(in value));
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
        writer.WritePropertyName(ToString(in value));
    }
    
    public static DateTime Parse(in ReadOnlySpan<char> str) {
        // Date portion
        DateOnly d = DateOnlyConv.Parse(in str);
        // Time portion
        TimeOnly t = TimeOnlyConv.Parse(str.Slice(11));
        return d.ToDateTime(t, DateTimeKind.Utc);
    }
    
    public static string ToString(in DateTime dt) {
        return ToStringUtc(TimeZoneInfo.ConvertTimeToUtc(dt));
    }
    
    public static string ToStringUtc(in DateTime dt) {
        Debug.Assert(dt.Kind == DateTimeKind.Utc);
        StrBuilder builder = new(stackalloc char[31]);
        DateOnlyConv.ToString(ref builder, DateOnly.FromDateTime(dt));
        builder.Append('T');
        TimeOnlyConv.ToString(ref builder, TimeOnly.FromDateTime(dt));
        builder.Append('Z');
        return builder.ToString();
    }

    [DoesNotReturn]
    private static DateTime ThrowJsonTokenTypeInvalid() {
        throw new JsonException("Cannot deserialize a non numeric non string token as a DateTime.");
    }
}