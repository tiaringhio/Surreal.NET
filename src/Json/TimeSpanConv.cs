using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Superpower;
using Superpower.Model;

namespace SurrealDB.Json;

public sealed class TimeSpanConv : JsonConverter<TimeSpan> {
    
    public static readonly Regex UnitTimeRegex = new(@"^([+-]?(?:\d*\.)\d+(?:[eE][+-]?\d+))(\w*)$", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant);
    
    
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        TimeSpan ts = reader.TokenType switch {
            JsonTokenType.None or JsonTokenType.Null => default,
            JsonTokenType.String or JsonTokenType.PropertyName => Parse(reader.GetString()),
            JsonTokenType.Number => TimeSpan.FromTicks(reader.GetInt64()),
            _ => ThrowJsonTokenTypeInvalid()
        };

        return ts;
    }

    public override TimeSpan ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
        writer.WriteStringValue(ToString(in value));
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
        writer.WritePropertyName(ToString(in value));
    }

    public static TimeSpan Parse(string? s) {
        return TryParse(s, out TimeSpan value) ? value : ThrowParseInvalid(s);
    }

    public static bool TryParse(string? s, out TimeSpan value) {
        if (String.IsNullOrEmpty(s)) {
            value = default;
            return false;
        }
        Result<TimeSpan> res1 = TimeParsers.SpecificTimeSpan(new TextSpan(s));
        if (res1.HasValue) {
            value = res1.Value;
            return true;
        }
        Result<TimeOnly> res2 = TimeParsers.IsoTime(new TextSpan(s));
        if (res2.HasValue) {
            value = res2.Value.ToTimeSpan();
            return true;
        }
        value = default;
        return false;
    }

    public static string ToString(in TimeSpan value) {
        return value.ToString($"{value.Days}d{value.Hours}h{value.Minutes}m{value.Seconds}s{value.Milliseconds}ms");
    }

    [DoesNotReturn]
    private static TimeSpan ThrowParseInvalid(string? s) {
        throw new ParseException($"Unable to parse TimeSpan from `{s}`");
    }


    [DoesNotReturn]
    private static TimeSpan ThrowJsonTokenTypeInvalid() {
        throw new JsonException("Cannot deserialize a non numeric non string token as a TimeSpan.");
    }
}