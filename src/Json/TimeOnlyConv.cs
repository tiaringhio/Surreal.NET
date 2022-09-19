using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Rustic;

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

    public static TimeOnly Parse(in ReadOnlySpan<char> str) {
        ReadOnlySpan<char> slc, rem = str;
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
        return new(t);
    }

    // Needs 18
    public static void ToString(ref StrBuilder builder, in TimeOnly dt) {
        
        dt.Hour.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
        builder.Append(':');
        dt.Minute.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
        builder.Append(':');
        dt.Second.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
        builder.Append('.');
        ExtractNanos(in dt).TryFormat(builder.AppendSpan(9), out _, "000000000", NumberFormatInfo.InvariantInfo);
    }
    
    public static string ToString(in TimeOnly ts) {
        StrBuilder builder = new(stackalloc char[19]);
        ToString(ref builder, in ts);
        return builder.ToString();
    }

    private static int ExtractNanos(in TimeOnly dt) {
        return (int)(dt.Ticks % TimeSpan.TicksPerSecond);
    }
    
    [DoesNotReturn]
    private TimeOnly ThrowJsonTokenTypeInvalid() {
        throw new JsonException("Cannot deserialize a non string token as a TimeOnly.");
    }
}