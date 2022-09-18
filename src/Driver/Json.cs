using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Rustic;

namespace Surreal.Net; 

/// <summary>
///     Converts keys to lower_snake_case for json serialization.
/// </summary>
public sealed class NameLowerSnakeCase : JsonNamingPolicy {
    private static readonly Lazy<NameLowerSnakeCase> s_instance = new(() => new());

    public static NameLowerSnakeCase Instance => s_instance.Value;

    public override string ConvertName(string name) {
        if (name == null) {
            throw new ArgumentNullException(nameof(name));
        }

        for (int i = 0; i < name.Length; i++) {
            if (!char.IsUpper(name[i])) {
                continue;
            }

            return ConvertNameSlow(name, i);
        }

        return name;
    }

    private static string ConvertNameSlow(
        ReadOnlySpan<char> name,
        int start) {
        int cap = name.Length + 5;
        using StrBuilder result = cap > 512 ? new(cap) : new(stackalloc char[cap]);
        TextInfo converter = CultureInfo.InvariantCulture.TextInfo;

        result.Append(name.Slice(0, start));
        bool prevUpper = true; // prevent leading _
        for (int pos = start; pos < name.Length; pos++) {
            char ch = name[pos];
            bool upper = char.IsUpper(ch);
            if (upper && !prevUpper) {
                result.Append('_');
            }

            result.Append(upper ? converter.ToLower(ch) : ch);
            prevUpper = upper;
        }

        return result.ToString();
    }
}

/// <summary>
/// Collection of repeatedly used constants.
/// </summary>
internal static class Constants {
    [ThreadStatic]
    private static JsonSerializerOptions? _jsonSerializerOptions;

    /// <summary>
    /// Creates or returns the shared <see cref="JsonSerializerOptions"/> instance for this thread.
    /// </summary>
    public static JsonSerializerOptions JsonOptions => _jsonSerializerOptions ??= CreateJsonOptions();

    /// <summary>
    /// Instantiates a new instance of <see cref="JsonSerializerOptions"/> with default settings.
    /// </summary>
    public static JsonSerializerOptions CreateJsonOptions() {
        return new() {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = false,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            // This was throwing an exception when set to JsonIgnoreCondition.Always
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            // TODO: Remove this when the server is fixed, see: https://github.com/surrealdb/surrealdb/issues/137
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IgnoreReadOnlyFields = false,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
            Converters = { new DecimalConv(), new DoubleConv(), new SingleConv(), new DateTimeConv(), new DateTimeOffsetConv(), new TimeSpanConv() }
        };
    }
}

internal static class SpecialNumbers {
    public const string NUM_NAN = "nan";
    public const string NUM_POSINF = "inf";
    public const string NUM_NEGINF = "-inf";
    public const string NUM_POSEPS = "eps";
    public const string NUM_NEGEPS = "-eps";
    public const string NUM_MAX = "max";
    public const string NUM_MIN = "min";

    public static float ToSingle(string special) {
        return special switch {
            NUM_NAN => Single.NaN,
            NUM_POSINF => Single.PositiveInfinity,
            NUM_NEGINF => Single.NegativeInfinity,
            NUM_POSEPS => Single.Epsilon,
            NUM_NEGEPS => -Single.Epsilon,
            NUM_MAX => Single.MaxValue,
            NUM_MIN => Single.MinValue,
            _ => default,
        };
    }

    public static string? ToSpecial(in float value) {
        if (Single.IsNaN(value)) {
            return NUM_NAN;
        }

        if (Single.IsPositiveInfinity(value)) {
            return NUM_POSINF;
        }

        if (Single.IsNegativeInfinity(value)) {
            return NUM_NEGINF;
        }

        if (Math.Abs(value) <= Single.Epsilon) {
            return value > 0 ? NUM_POSEPS : NUM_NEGEPS;
        }

        if (value >= Single.MaxValue) {
            return NUM_MAX;
        }

        if (value <= Single.MinValue) {
            return NUM_MIN;
        }

        return null;
    }


    public static double ToDouble(string special) {
        return special switch {
            NUM_NAN => Double.NaN,
            NUM_POSINF => Double.PositiveInfinity,
            NUM_NEGINF => Double.NegativeInfinity,
            NUM_POSEPS => Double.Epsilon,
            NUM_NEGEPS => -Double.Epsilon,
            NUM_MAX => Double.MaxValue,
            NUM_MIN => Double.MinValue,
            _ => default,
        };
    }

    public static string? ToSpecial(in double value) {
        if (Double.IsNaN(value)) {
            return NUM_NAN;
        }

        if (Double.IsPositiveInfinity(value)) {
            return NUM_POSINF;
        }

        if (Double.IsNegativeInfinity(value)) {
            return NUM_NEGINF;
        }

        if (Math.Abs(value) <= Double.Epsilon) {
            return value > 0 ? NUM_POSEPS : NUM_NEGEPS;
        }

        if (value >= Double.MaxValue) {
            return NUM_MAX;
        }

        if (value <= Double.MinValue) {
            return NUM_MIN;
        }

        return null;
    }

    public static decimal ToDecimal(string str) {
        if (str == NUM_MAX) {
            return Decimal.MaxValue;
        }

        if (str == NUM_MIN) {
            return Decimal.MinValue;
        }

        return default;
    }
    public static string? ToSpecial(in decimal value) {
        if (value >= Decimal.MaxValue) {
            return NUM_MAX;
        }

        if (value <= Decimal.MinValue) {
            return NUM_MIN;
        }

        return null;
    }

}

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


public sealed class DecimalConv : JsonConverter<decimal> {
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        if (reader.TokenType is JsonTokenType.Null or JsonTokenType.None) {
            return default;
        }

        if (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName) {
            var str = reader.GetString();
            if (String.IsNullOrEmpty(str)) {
                return default;
            }

            decimal v = SpecialNumbers.ToDecimal(str);
            if (v != default) {
                return v;
            }

            return Decimal.Parse(str, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
        }

        if (reader.TokenType is JsonTokenType.Number) {
            return reader.GetDecimal();
        }

        throw new JsonException("Could not parse the number.");
    }

    public override decimal ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) {
        string? str = SpecialNumbers.ToSpecial(in value);
        if (str is not null) {
            writer.WriteStringValue(str.AsSpan());
            return;
        }

        writer.WriteNumberValue(value);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) {
        string? str = SpecialNumbers.ToSpecial(in value);
        if (str is not null) {
            writer.WritePropertyName(str.AsSpan());
            return;
        }

        writer.WritePropertyName(value.ToString(null, NumberFormatInfo.InvariantInfo).AsSpan());
    }
}

public sealed class DateTimeConv : JsonConverter<DateTime> {
    public static string FromDateTime(in DateTime dt) {
        return FromUtc(TimeZoneInfo.ConvertTimeToUtc(dt));
    }
    
    public static string FromUtc(in DateTime dt) {
        Debug.Assert(dt.Kind == DateTimeKind.Utc);
        StrBuilder builder = new(stackalloc char[31]);
        dt.Year.TryFormat(builder.AppendSpan(4), out _, "0000", NumberFormatInfo.InvariantInfo);
        builder.Append('-');
        dt.Month.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
        builder.Append('-');
        dt.Day.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
        builder.Append('T');
        dt.Hour.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
        builder.Append(':');
        dt.Minute.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
        builder.Append(':');
        dt.Second.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
        builder.Append('.');
        ExtractNanos(in dt).TryFormat(builder.AppendSpan(9), out _, "000000000", NumberFormatInfo.InvariantInfo);
        builder.Append('Z');
        return builder.ToString();
    }

    private static int ExtractNanos(in DateTime dt) {
        return (int)(dt.Ticks % TimeSpan.TicksPerSecond);
    }

    public static DateTime FromString(in ReadOnlySpan<char> str) {
        ReadOnlySpan<char> slc, rem = str;
        // Date portion
        slc = rem.Slice(0, 4);
        rem = rem.Slice(4);
        int y = Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        slc = rem.Slice(1, 2);
        rem = rem.Slice(3);
        int m = Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        slc = rem.Slice(1, 2);
        rem = rem.Slice(3);
        int d = Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        // Time portion
        long t = 0;
        slc = rem.Slice(1, 2);
        rem = rem.Slice(3);
        t += TimeSpan.TicksPerHour * Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        slc = rem.Slice(1, 2);
        rem = rem.Slice(3);
        t += TimeSpan.TicksPerMinute * Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        slc = rem.Slice(1, 2);
        rem = rem.Slice(3);
        t += TimeSpan.TicksPerSecond * Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        slc = rem.Slice(1, 9);
        rem = rem.Slice(10);
        t += Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        Debug.Assert(t <= 863999999999L);
        return new DateOnly(y, m, d).ToDateTime(new(t));
    }
    
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        DateTime dt = reader.TokenType switch {
            JsonTokenType.Null or JsonTokenType.None => default,
            JsonTokenType.String or JsonTokenType.PropertyName => FromString(reader.GetString()),
            JsonTokenType.Number => DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64()).UtcDateTime,
            _ => ThrowJsonTokenTypeInvalid()
        };

        return dt;
    }

    public override DateTime ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
        writer.WriteStringValue(FromDateTime(in value));
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) {
        writer.WritePropertyName(FromDateTime(in value));
    }

    [DoesNotReturn]
    private static DateTime ThrowJsonTokenTypeInvalid() {
        throw new JsonException("Cannot deserialize a non numeric non string token as a DateTime.");
    }
}

public sealed class DateTimeOffsetConv : JsonConverter<DateTimeOffset> {
    public static string FromDateTime(in DateTimeOffset dt) {
        return DateTimeConv.FromUtc(dt.UtcDateTime);
    }
    
    public static DateTimeOffset FromString(in ReadOnlySpan<char> str) {
        return DateTimeConv.FromString(str);
    }
    
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        DateTimeOffset dt = reader.TokenType switch {
            JsonTokenType.Null or JsonTokenType.None => default,
            JsonTokenType.String or JsonTokenType.PropertyName => FromString(reader.GetString()),
            JsonTokenType.Number => DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64()),
            _ => ThrowJsonTokenInvalid()
        };

        return dt;
    }

    public override DateTimeOffset ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) {
        writer.WriteStringValue(FromDateTime(in value));
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) {
        writer.WritePropertyName(FromDateTime(in value));
    }
    
    [DoesNotReturn]
    private static DateTimeOffset ThrowJsonTokenInvalid() {
        throw new JsonException("Cannot deserialize a non numeric non string token as a DateTime.");
    }
}

public sealed class TimeSpanConv : JsonConverter<TimeSpan> {
    public const string ISO_8061_FORMAT = "HH:mm:ss.fffffff";

    public static readonly Regex UnitTimeRegex = new(@"^([+-]?(?:\d*\.)\d+(?:[eE][+-]?\d+))(\w*)$", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant);

    public static string FromTimeSpan(in TimeSpan ts) {
        return ts.ToString(ISO_8061_FORMAT, DateTimeFormatInfo.InvariantInfo);
    }
    
    public static TimeSpan FromString(string? str) {
        if (String.IsNullOrEmpty(str)) {
            return default;
        }
        if (TimeSpan.TryParseExact(str.AsSpan(), ISO_8061_FORMAT, DateTimeFormatInfo.InvariantInfo, TimeSpanStyles.None, out TimeSpan ts)) {
            return ts;
        }

        if (ParseUnitTime(str, out TimeSpan fromMilliseconds)) {
            return fromMilliseconds;
        }

        return ThrowFormatInvalid();
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
            {
                ts = TimeSpan.FromTicks(lng);
                return true;
            }
        }

        double dbl = Double.Parse(val, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
        if (unt.Equals("µs", StringComparison.OrdinalIgnoreCase)
         || unt.Equals("us", StringComparison.OrdinalIgnoreCase)) {
            {
                ts = TimeSpan.FromMilliseconds(dbl * 1000.0);
                return true;
            }
        }

        if (unt.Equals("ms", StringComparison.OrdinalIgnoreCase)) {
            {
                ts = TimeSpan.FromMilliseconds(dbl);
                return true;
            }
        }

        if (unt.Equals("s", StringComparison.OrdinalIgnoreCase)) {
            {
                ts = TimeSpan.FromSeconds(dbl);
                return true;
            }
        }

        if (unt.Equals("m", StringComparison.OrdinalIgnoreCase)) {
            {
                ts = TimeSpan.FromMinutes(dbl);
                return true;
            }
        }

        if (unt.Equals("h", StringComparison.OrdinalIgnoreCase)) {
            {
                ts = TimeSpan.FromHours(dbl);
                return true;
            }
        }

        if (unt.Equals("d", StringComparison.OrdinalIgnoreCase)) {
            {
                ts = TimeSpan.FromDays(dbl);
                return true;
            }
        }

        ThrowFormatUnitUnknown(unt);
        ts = default;
        return false;
    }

    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        TimeSpan ts = reader.TokenType switch {
            JsonTokenType.None or JsonTokenType.Null => default,
            JsonTokenType.String or JsonTokenType.PropertyName => FromString(reader.GetString()),
            JsonTokenType.Number => TimeSpan.FromTicks(reader.GetInt64()),
            _ => ThrowJsonTokenTypeInvalid()
        };

        return ts;
    }

    public override TimeSpan ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
        writer.WriteStringValue(FromTimeSpan(in value));
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) {
        writer.WritePropertyName(FromTimeSpan(in value));
    }
    
    [DoesNotReturn]
    private static TimeSpan ThrowFormatInvalid() {
        throw new FormatException("Invalid format for a TimeSpan, allowed are ISO8061 and value unit combination.");
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
