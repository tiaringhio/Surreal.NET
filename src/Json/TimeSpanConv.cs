using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

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

    public static string ToString(in TimeSpan ts) {
        return TimeOnlyConv.ToString( TimeOnly.FromTimeSpan(ts));
    }
    
    public static TimeSpan Parse(string? str) {
        if (String.IsNullOrEmpty(str)) {
            return default;
        }

        if (ParseUnitTime(str, out TimeSpan ts)) {
            return ts;
        }

        return TimeOnlyConv.Parse(str).ToTimeSpan();
    }

    private static bool ParseUnitTime(string str, out TimeSpan ts) {
        Match match = UnitTimeRegex.Match(str);
        if (!match.Success) {
            ts = default;
            return false;
        }

        ReadOnlySpan<char> val = match.Groups[1].ValueSpan;
        ReadOnlySpan<char> unt = match.Groups[2].ValueSpan;
        if (unt.IsEmpty || unt.Equals("ns", StringComparison.OrdinalIgnoreCase)) {
            long lng = Int64.Parse(val, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
            ts = TimeSpan.FromTicks(lng);
            return true;
        }

        double dbl = Double.Parse(val, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
        if (unt.Equals("µs", StringComparison.OrdinalIgnoreCase)
         || unt.Equals("us", StringComparison.OrdinalIgnoreCase)) {
            ts = TimeSpan.FromMilliseconds(dbl * 1000.0);
            return true;
        }

        if (unt.Equals("ms", StringComparison.OrdinalIgnoreCase)) {
            ts = TimeSpan.FromMilliseconds(dbl);
            return true;
        }

        if (unt.Equals("s", StringComparison.OrdinalIgnoreCase)) {
            ts = TimeSpan.FromSeconds(dbl);
            return true;
        }

        if (unt.Equals("m", StringComparison.OrdinalIgnoreCase)) {
            ts = TimeSpan.FromMinutes(dbl);
            return true;
        }

        if (unt.Equals("h", StringComparison.OrdinalIgnoreCase)) {
            ts = TimeSpan.FromHours(dbl);
            return true;
        }

        if (unt.Equals("d", StringComparison.OrdinalIgnoreCase)) {
            ts = TimeSpan.FromDays(dbl);
            return true;
        }

        ThrowFormatUnitUnknown(unt);
        ts = default;
        return false;
    }

    [DoesNotReturn]
    private static void ThrowFormatUnitUnknown(ReadOnlySpan<char> unt) {
        throw new FormatException($"Invalid TimeSpan unit `{unt}`");
    }
    
    [DoesNotReturn]
    private static TimeSpan ThrowJsonTokenTypeInvalid() {
        throw new JsonException("Cannot deserialize a non numeric non string token as a TimeSpan.");
    }
}