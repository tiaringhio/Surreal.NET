using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Rustic;

namespace SurrealDB.Json;

public sealed class DateOnlyConv : JsonConverter<DateOnly> {
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return reader.TokenType switch {
            JsonTokenType.None or JsonTokenType.Null => default,
            JsonTokenType.String or JsonTokenType.PropertyName => Parse(reader.GetString()),
            _ => ThrowJsonTokenTypeInvalid()
        };
    }

    public override DateOnly ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return Read(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) {
        writer.WriteStringValue(ToString(in value));
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) {
        writer.WritePropertyName(ToString(in value));
    }

    public static DateOnly Parse(in ReadOnlySpan<char> str) {
        ReadOnlySpan<char> slc, rem = str;
        slc = rem.Slice(0, 4);
        rem = rem.Slice(4);
        int y = Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        slc = rem.Slice(1, 2);
        rem = rem.Slice(3);
        int m = Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        slc = rem.Slice(1, 2);
        rem = rem.Slice(3);
        int d = Int32.Parse(slc, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
        return new(y, m , d);
    }

    // Needs 10 chars
    public static void ToString(ref StrBuilder builder, in DateOnly dt) {
        dt.Year.TryFormat(builder.AppendSpan(4), out _, "0000", NumberFormatInfo.InvariantInfo);
        builder.Append('-');
        dt.Month.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
        builder.Append('-');
        dt.Day.TryFormat(builder.AppendSpan(2), out _, "00", NumberFormatInfo.InvariantInfo);
    }

    public static string ToString(in DateOnly dt) {
        StrBuilder builder = new(stackalloc char[11]);
        ToString(ref builder, dt);
        return builder.ToString();
    }
    
    [DoesNotReturn]
    private DateOnly ThrowJsonTokenTypeInvalid() {
        throw new JsonException("Cannot deserialize a non string token as a DateOnly.");
    }
}