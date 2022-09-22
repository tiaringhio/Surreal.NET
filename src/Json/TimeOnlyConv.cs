using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Rustic;

using Superpower;
using Superpower.Model;

namespace SurrealDB.Json;

public sealed class TimeOnlyConv : JsonConverter<TimeOnly> {
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return reader.TokenType switch {
            JsonTokenType.None or JsonTokenType.Null => default,
            JsonTokenType.String or JsonTokenType.PropertyName => Parse(reader.GetString()),
            _ => ThrowJsonTokenTypeInvalid()
        };
    }

    public override TimeOnly ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) {
        writer.WriteStringValue(ToString(in value));
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) {
        writer.WritePropertyName(ToString(in value));
    }

    public static TimeOnly Parse(string? s) {
        return TryParse(s, out TimeOnly value) ? value : ThrowParseInvalid(s);
    }

    public static bool TryParse(string? s, out TimeOnly value) {
        if (String.IsNullOrEmpty(s)) {
            return false;
        }
        Result<TimeOnly> res = TimeParsers.IsoTime(new TextSpan(s));
        value = res.HasValue ? res.Value : default;
        return res.HasValue;
    }

    public static string ToString(in TimeOnly value) {
        return $"{value.Hour.ToString("D2")}:{value.Minute.ToString("D2")}:{value.Second.ToString("D2")}.{value.FractionString()}";
    }

    [DoesNotReturn]
    private static TimeOnly ThrowParseInvalid(string? s) {
        throw new ParseException($"Unable to parse TimeOnly from `{s}`");
    }


    [DoesNotReturn]
    private TimeOnly ThrowJsonTokenTypeInvalid() {
        throw new JsonException("Cannot deserialize a non string token as a TimeOnly.");
    }
}