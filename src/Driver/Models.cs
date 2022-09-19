using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Surreal.Net;

/// <summary>
///     Indicates a table or a specific record.
/// </summary>
/// <remarks>
///     `table_name:record_id`
/// </remarks>
[JsonConverter(typeof(Converter))]
public readonly struct SurrealThing : IEquatable<SurrealThing> {
    public const char CHAR_SEP = ':';
    public const char CHAR_PRE = '⟨';
    public const char CHAR_SUF = '⟩';
    
    private readonly int _split;

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        SurrealThing(
            int split,
            string thing) {
        _split = split;
        Thing = thing;
    }
    
    /// <summary>
    /// Returns the underlying string.
    /// </summary>
    public string Thing { get; }

    /// <summary>
    /// Returns the Table part of the Thing
    /// </summary>
    public ReadOnlySpan<char> Table => Thing.AsSpan(0, _split);

    /// <summary>
    /// Returns the Key part of the Thing.
    /// </summary>
    public ReadOnlySpan<char> Key => GetKeyOffset(out int rec) ? Thing.AsSpan(rec) : default;

    /// <summary>
    /// If the <see cref="Key"/> is present returns the <see cref="Table"/> part including the separator; otherwise returns the <see cref="Table"/>.
    /// </summary>
    public ReadOnlySpan<char> TableAndSeparator => GetKeyOffset(out int rec) ? Thing.AsSpan(0, rec) : Thing;

    public bool HasKey => _split < Length;

    public int Length => Thing.Length;

    /// <summary>
    /// Indicates whether the <see cref="Key"/> is escaped. true if no <see cref="Key"/> is present.
    /// </summary>
    public bool IsKeyEscaped => GetKeyOffset(out int rec) ? Thing[rec] == CHAR_PRE && Thing[Thing.Length - 1] == CHAR_SUF : true;

    /// <summary>
    /// Returns the unescaped key, if tne key is escaped
    /// </summary>
    public bool TryUnescapeKey(out ReadOnlySpan<char> key) {
        if (!GetKeyOffset(out int off) || !IsKeyEscaped) {
            key = default;
            return false;
        }

        int escOff = off + 1;
        key = Thing.AsSpan(escOff, Thing.Length - escOff - 1);
        return true;
    }

    /// <summary>
    /// Escapes the <see cref="SurrealThing"/> if not already <see cref="IsKeyEscaped"/>.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SurrealThing Escape() {
        return IsKeyEscaped ? this : new(_split, $"{TableAndSeparator}{CHAR_PRE}{Key}{CHAR_SUF}");
    }

    /// <summary>
    /// Uneescapes the <see cref="SurrealThing"/> if not already <see cref="IsKeyEscaped"/>.
    /// </summary>
    /// <returns></returns>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SurrealThing Unescape() {
        return TryUnescapeKey(out ReadOnlySpan<char> key) ? new(_split, $"{TableAndSeparator}{key}") : this;
    }

    [Pure]
    private bool GetKeyOffset(out int off) {
        off = _split + 1;
        return HasKey;
    }

    public static SurrealThing From(string? thing) {
        if (string.IsNullOrEmpty(thing)) {
            return default;
        }

        int split = thing.IndexOf(CHAR_SEP);
        return new(split <= 0 ? thing.Length : split, thing);
    }

    public static SurrealThing From(
        in ReadOnlySpan<char> table,
        in ReadOnlySpan<char> key) {
        return From($"{table}:{key}");
    }

    public SurrealThing WithTable(in ReadOnlySpan<char> table) {
        int keyOffset = table.Length + 1;
        int chars = keyOffset + Key.Length;
        Span<char> builder = stackalloc char[chars];
        table.CopyTo(builder);
        builder[table.Length] = CHAR_SEP;
        Key.CopyTo(builder.Slice(keyOffset));
        return new(table.Length, builder.ToString());
    }

    public SurrealThing WithKey(in ReadOnlySpan<char> key) {
        int keyOffset = Table.Length + 1;
        int chars = keyOffset + key.Length;
        Span<char> builder = stackalloc char[chars];
        Table.CopyTo(builder);
        builder[Table.Length] = ':';
        key.CopyTo(builder.Slice(keyOffset));
        return new(Table.Length, builder.ToString());
    }

    public static implicit operator SurrealThing(in string? thing) {
        return From(thing);
    }

    // Double implicit operators can result in syntax problems, so we use the explicit operator instead.
    public static explicit operator string(in SurrealThing thing) {
        return thing.Thing;
    }

    public bool Equals(SurrealThing other) {
        return Thing == other.Thing;
    }

    public override bool Equals(object? obj) {
        return obj is SurrealThing other && Equals(other);
    }

    public override int GetHashCode() {
        return Thing.GetHashCode();
    }

    public override string ToString() {
        return (string)Converter.EscapeComplexCharactersIfRequired(in this);
    }

    public sealed class Converter : JsonConverter<SurrealThing> {
        public override SurrealThing Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return reader.GetString();
        }

        public override SurrealThing ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, SurrealThing value, JsonSerializerOptions options) { 
            writer.WriteStringValue((string)EscapeComplexCharactersIfRequired(in value));
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, SurrealThing value, JsonSerializerOptions options) {
            writer.WritePropertyName((string)EscapeComplexCharactersIfRequired(in value));
        }

        internal static SurrealThing EscapeComplexCharactersIfRequired(in SurrealThing thing) {
            if (thing.IsKeyEscaped || !ContainsComplexCharacters(in thing)) {
                return thing;
            }

            return thing.Escape();
        }

        private static bool ContainsComplexCharacters(in SurrealThing thing) {
            if (!thing.GetKeyOffset(out int rec)) {
                // This Thing is not split
                return false;
            }
            ReadOnlySpan<char> text = (string)thing;
            int len = text.Length;

            if (text[rec] == CHAR_PRE && text[len - 1] == CHAR_SUF) {
                // Already escaped, don't escape it again.
                return false;
            }

            for (int i = rec; i < len; i++) {
                char ch = text[i];
                if (!char.IsLetterOrDigit(ch) && ch != '_') {
                    return true;
                }
            }

            return false;
        }
    }
}

public interface ISurrealResponse {
    public bool IsOk { get; }

    public bool IsError { get; }

    public bool TryGetError(out SurrealError error);

    public bool TryGetResult(out SurrealResult result);

    public bool TryGetResult(
        out SurrealResult result,
        out SurrealError error);
}

/// <summary>
///     The response from a query to the Surreal database via rest.
/// </summary>
public readonly struct SurrealRestResponse : ISurrealResponse {
    private readonly string? _status;
    private readonly string? _detail;
    private readonly string? _description;
    private readonly JsonElement _result;

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        SurrealRestResponse(
            string? time,
            string? status,
            string? description,
            string? detail,
            JsonElement result) {
        Time = time;
        _status = status;
        _detail = detail;
        _result = result;
        _description = description;
    }

    public string? Time { get; }

    public bool IsOk => string.Equals(_status, "ok", StringComparison.OrdinalIgnoreCase);

    public bool IsError => !IsOk;

    public bool TryGetError(out SurrealError error) {
        if (IsOk) {
            error = default;
            return false;
        }

        error = new(1, $"{_detail}: {_description}");
        return true;
    }

    public bool TryGetResult(out SurrealResult result) {
        if (IsError) {
            result = default;
            return false;
        }

        result = SurrealResult.From(_result);
        return true;
    }

    public bool TryGetResult(
        out SurrealResult result,
        out SurrealError error) {
        if (IsError) {
            result = default;
            error = new(1, _detail);
            ;
            return false;
        }

        result = SurrealResult.From(_result);
        error = default;
        return true;
    }

    /// <summary>
    /// Parses a <see cref="HttpResponseMessage"/> containing JSON to a <see cref="SurrealRestResponse"/>. 
    /// </summary>
    public static async Task<SurrealRestResponse> From(
        HttpResponseMessage msg,
        CancellationToken ct = default) {
        Stream stream = await msg.Content.ReadAsStreamAsync(ct);
        if (msg.StatusCode != HttpStatusCode.OK) {
            HttpError? err = await JsonSerializer.DeserializeAsync<HttpError>(stream, Constants.JsonOptions, ct);
            return From(err);
        }
        
        if (await PeekIsEmpty(stream, ct)) {
            // Success and empty message -> invalid json
            return EmptyOk;
        }
        
        var docs = await JsonSerializer.DeserializeAsync<List<HttpSuccess>>(stream, Constants.JsonOptions, ct);
        var doc = docs?.FirstOrDefault(e => e.result.ValueKind != JsonValueKind.Null);


        return From(doc);
    }

    /// <summary>
    /// Attempts to peek the next byte of the stream. 
    /// </summary>
    /// <remarks>
    /// Resets the stream to the original position.
    /// </remarks>
    private static async Task<bool> PeekIsEmpty(
        Stream stream,
        CancellationToken ct) {
        Debug.Assert(stream.CanSeek && stream.CanRead);
        using IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(1);
        // This is more efficient, then ReadByte.
        // Async, because this is the first request to the networkstream, thus no readahead is possible.
        int read = await stream.ReadAsync(buffer.Memory.Slice(0, 1), ct);
        stream.Seek(-read, SeekOrigin.Current);
        return read <= 0;
    }

    public static SurrealRestResponse EmptyOk => new(null, "ok", null, null, default);

    private static SurrealRestResponse From(HttpError? error) {
        return new(null, "HTTP_ERR", error?.description, error?.details, default);
    }

    private static SurrealRestResponse From(HttpSuccess? success) {
        return new(success?.time, success?.status, null, null, success?.result ?? default);
    }

    private record HttpError(
        int code,
        string details,
        string description,
        string information);

    private record HttpSuccess(
        string time,
        string status,
        JsonElement result);
}

public static class SurrealRestClientExtensions {
    [DebuggerStepThrough]
    public static Task<SurrealRestResponse> ToSurreal(this HttpResponseMessage msg) {
        return SurrealRestResponse.From(msg);
    }
}

/// <summary>
///     The response from a query to the Surreal database via rpc.
/// </summary>
public readonly struct SurrealRpcResponse : ISurrealResponse {
    private readonly SurrealError _error;

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        SurrealRpcResponse(
            string id,
            SurrealError error,
            SurrealResult result) {
        Id = id;
        _error = error;
        UncheckedResult = result;
    }

    public string Id { get; }

    public bool IsOk => _error.Code == 0;
    public bool IsError => _error.Code != 0;

    public SurrealResult UncheckedResult { get; }
    public SurrealError UncheckedError => _error;

    public bool TryGetError(out SurrealError error) {
        error = _error;
        return IsError;
    }

    public bool TryGetResult(out SurrealResult result) {
        result = UncheckedResult;
        return IsOk;
    }

    public bool TryGetResult(
        out SurrealResult result,
        out SurrealError error) {
        result = UncheckedResult;
        error = _error;
        return IsOk;
    }

    public void Deconstruct(
        out SurrealResult result,
        out SurrealError error) {
        (result, error) = (UncheckedResult, _error);
    }

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        static SurrealRpcResponse From(in RpcResponse rsp) {
        if (rsp.Id is null) {
            ThrowIdMissing();
        }

        if (rsp.Error.HasValue) {
            RpcError err = rsp.Error.Value;
            return new(rsp.Id, new(err.Code, err.Message), default);
        }
        
        // SurrealDB likes to returns a list of one result. Unbox this response, to conform with the REST client
        SurrealResult res = SurrealResult.From(IntoSingle(rsp.Result));
        return new(rsp.Id, default, res);
    }

    private static JsonElement IntoSingle(in JsonElement root) {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() > 1) {
            return root;
        }
        
        var en = root.EnumerateArray();
        while (en.MoveNext()) {
            JsonElement cur = en.Current;
            // Return the first not null element
            if (cur.ValueKind is not JsonValueKind.Null or JsonValueKind.Undefined) {
                return cur;
            }
        }
        // No content in the array.
        return default;
    }
    
    [DoesNotReturn]
    private static void ThrowIdMissing() {
        throw new InvalidOperationException("Response does not have an id.");
    }
}

public static class SurrealRpcClientExtensions {
#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        static SurrealRpcResponse ToSurreal(this RpcResponse rsp) => SurrealRpcResponse.From(in rsp);

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        static async Task<SurrealRpcResponse> ToSurreal(this Task<RpcResponse> rsp) => SurrealRpcResponse.From(await rsp);
}

public enum SurrealResultKind : byte {
    Object,
    Array,
    None,
    String,
    SignedInteger,
    UnsignedInteger,
    Float,
    Boolean
}

/// <summary>
///     The result of a successful query to the Surreal database.
/// </summary>
public readonly struct SurrealResult : IEquatable<SurrealResult>, IComparable<SurrealResult> {
    private readonly JsonElement _json;
    private readonly object? _sentinelOrValue;
    private readonly long _int64ValueField;

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        SurrealResult(
            JsonElement json,
            object? sentinelOrValue) {
        _json = json;
        _sentinelOrValue = sentinelOrValue;
        _int64ValueField = 0;
    }

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        SurrealResult(
            JsonElement json,
            object? sentinelOrValue,
            long int64ValueField) {
        _json = json;
        _sentinelOrValue = sentinelOrValue;
        _int64ValueField = int64ValueField;
    }

    public JsonElement Inner => _json;

    public bool TryGetObject<T>([NotNullWhen(true)] out T? obj) {
        obj = _json.Deserialize<T>(Constants.JsonOptions);
        return !EqualityComparer<T>.Default.Equals(default, obj);
    }

    public IEnumerable<T> GetArray<T>() {
        if (_json.ValueKind != JsonValueKind.Array || _json.GetArrayLength() <= 0) {
            yield break;
        }

        var en = _json.EnumerateArray();
        while (en.MoveNext()) {
            T? v = en.Current.Deserialize<T>(Constants.JsonOptions);
            if (!EqualityComparer<T>.Default.Equals(default, v)) {
                yield return v!;
            }
        }
    }

    public bool TryGetValue([NotNullWhen(true)] out string? value) {
        bool isString = GetKind() == SurrealResultKind.String;
        value = isString ? (string)_sentinelOrValue! : null;
        return isString;
    }

    public bool TryGetValue(out long value) {
        bool isInt = GetKind() == SurrealResultKind.SignedInteger;
        value = isInt ? _int64ValueField : 0;
        return isInt;
    }

    public bool TryGetValue(out ulong value) {
        bool isInt = GetKind() == SurrealResultKind.SignedInteger;
        long data = _int64ValueField;
        value = isInt ? Unsafe.As<long, ulong>(ref data) : 0;
        return isInt;
    }

    public bool TryGetValue(out double value) {
        bool isFloat = GetKind() == SurrealResultKind.Float;
        long data = _int64ValueField;
        value = isFloat ? Unsafe.As<long, double>(ref data) : 0;
        return isFloat;
    }

    public bool TryGetValue(out bool value) {
        bool isBoolean = GetKind() == SurrealResultKind.Boolean;
        value = isBoolean && _int64ValueField != FalseValue;
        return value;
    }

    // Below is the logic determining the type of the boxed value in the result.
    // The type is primarily determined by the presence of a sentinel.
    // Both strings and documents make use of the sentinel field as a value field,
    // In this case the valueField determines the type.
    private static readonly object ArraySentinel = new();
    private static readonly object ObjectSentinel = new();
    private static readonly object NoneSentinel = new();
    private static readonly object SignedIntegerSentinel = new();
    private static readonly object UnsignedIntegerSentinel = new();
    private static readonly object FloatSentinel = new();
    private static readonly object BooleanSentinel = new();

    private const long TrueValue = 1;
    private const long FalseValue = 0;

    public static SurrealResult From(in JsonElement json) {
        return json.ValueKind switch {
            JsonValueKind.Undefined => new(json, NoneSentinel),
            JsonValueKind.Object =>  new(json, ObjectSentinel),
            JsonValueKind.Array => new(json, ArraySentinel),
            JsonValueKind.String => new(json, json.GetString()),
            JsonValueKind.Number => FromNumber(json),
            JsonValueKind.True => new(json, BooleanSentinel, TrueValue),
            JsonValueKind.False => new(json, BooleanSentinel, FalseValue),
            JsonValueKind.Null => new(json, NoneSentinel),
            _ => ThrowUnknownJsonValueKind(json),
        };
    }

    private static SurrealResult FromNumber(in JsonElement json) {
        if (json.TryGetInt64(out long signed)) {
            return new(json, SignedIntegerSentinel, signed);
        }

        if (json.TryGetUInt64(out ulong unsigned)) {
            return new(json, UnsignedIntegerSentinel, Unsafe.As<ulong, long>(ref unsigned));
        }

        if (json.TryGetDouble(out double dbl)) {
            return new(json, FloatSentinel, Unsafe.As<double, long>(ref dbl));
        }

        return new(json, NoneSentinel);
    }

    private SurrealResultKind GetKind() {

        if (ReferenceEquals(ObjectSentinel, _sentinelOrValue)) {
            return SurrealResultKind.Object;
        }

        if (ReferenceEquals(ArraySentinel, _sentinelOrValue)) {
            return SurrealResultKind.Array;
        }

        if (ReferenceEquals(NoneSentinel, _sentinelOrValue)) {
            return SurrealResultKind.None;
        }

        if (ReferenceEquals(SignedIntegerSentinel, _sentinelOrValue)) {
            return SurrealResultKind.SignedInteger;
        }

        if (ReferenceEquals(UnsignedIntegerSentinel, _sentinelOrValue)) {
            return SurrealResultKind.UnsignedInteger;
        }

        if (ReferenceEquals(FloatSentinel, _sentinelOrValue)) {
            return SurrealResultKind.Float;
        }

        if (ReferenceEquals(BooleanSentinel, _sentinelOrValue)) {
            return SurrealResultKind.Boolean;
        }
        
        if (_sentinelOrValue is string) {
            return SurrealResultKind.String;
        }

        Debug.Assert(false); // Should not happen, but is not fatal; None covers all edge cases.
        return SurrealResultKind.None;
    }

    [DoesNotReturn, DebuggerStepThrough,]
    private static SurrealResult ThrowUnknownJsonValueKind(JsonElement json) {
        throw new ArgumentOutOfRangeException(nameof(json), json.ValueKind, "Unknown value kind.");
    }

    // Below is the implementation for the comparison and equality logic,
    // as well as operator overloads and conversion logic for IConvertible.

    public bool Equals(in SurrealResult other) {
        // Fastest check for inequality, is via the value field.
        if (_int64ValueField != other._int64ValueField) {
            return false;
        }

        // More expensive check for the type of the boxed value.
        SurrealResultKind kind = GetKind();

        // Most expensive check requires unboxing of the value.
        return kind == other.GetKind() && EqualsUnboxed(in other, in kind);
    }

    private bool EqualsUnboxed(
        in SurrealResult other,
        in SurrealResultKind kind) {
        return kind switch {
            SurrealResultKind.Object or SurrealResultKind.None => EqualityComparer<JsonElement>.Default.Equals(
                _json,
                other._json
            ),
            SurrealResultKind.String => string.Equals((string)_sentinelOrValue!, (string)other._sentinelOrValue!),
            // Due to the unsafe case we are still able to use the operator and do not need to cast to compare structs.
            _ => _int64ValueField == other._int64ValueField,
        };
    }

    // The struct is big, do not copy if not necessary!
    bool IEquatable<SurrealResult>.Equals(SurrealResult other) {
        return Equals(in other);
    }

    public override bool Equals(object? obj) {
        return obj is SurrealResult other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(_json.ValueKind, _sentinelOrValue, _int64ValueField);
    }

    public static bool operator ==(
        in SurrealResult left,
        in SurrealResult right) {
        return left.Equals(in right);
    }

    public static bool operator !=(
        in SurrealResult left,
        in SurrealResult right) {
        return !left.Equals(in right);
    }


    public int CompareTo(in SurrealResult other) {
        SurrealResultKind thisKind = GetKind();
        SurrealResultKind otherKind = other.GetKind();

        long thisValue = _int64ValueField;
        long otherValue = other._int64ValueField;

        return (thisKind, otherKind) switch {
            (SurrealResultKind.SignedInteger, SurrealResultKind.SignedInteger) => thisValue.CompareTo(otherValue),
            (SurrealResultKind.SignedInteger, SurrealResultKind.UnsignedInteger) =>
                ((double)thisValue).CompareTo(Unsafe.As<long, ulong>(ref otherValue)),
            (SurrealResultKind.SignedInteger, SurrealResultKind.Float) =>
                ((double)thisValue).CompareTo(Unsafe.As<long, double>(ref otherValue)),

            (SurrealResultKind.UnsignedInteger, SurrealResultKind.SignedInteger) =>
                ((double)Unsafe.As<long, ulong>(ref thisValue)).CompareTo(otherValue),
            (SurrealResultKind.UnsignedInteger, SurrealResultKind.UnsignedInteger) =>
                Unsafe.As<long, ulong>(ref thisValue).CompareTo(Unsafe.As<long, ulong>(ref otherValue)),
            (SurrealResultKind.UnsignedInteger, SurrealResultKind.Float) =>
                ((double)Unsafe.As<long, ulong>(ref thisValue)).CompareTo(Unsafe.As<long, double>(ref otherValue)),

            (SurrealResultKind.Float, SurrealResultKind.SignedInteger) =>
                Unsafe.As<long, double>(ref thisValue).CompareTo(otherValue),
            (SurrealResultKind.Float, SurrealResultKind.UnsignedInteger) =>
                Unsafe.As<long, double>(ref thisValue).CompareTo(Unsafe.As<long, ulong>(ref otherValue)),
            (SurrealResultKind.Float, SurrealResultKind.Float) =>
                Unsafe.As<long, double>(ref thisValue).CompareTo(Unsafe.As<long, double>(ref otherValue)),

            _ => ThrowInvalidCompareTypes(),
        };
    }

    // The struct is big, do not copy if not necessary!
    int IComparable<SurrealResult>.CompareTo(SurrealResult other) {
        return CompareTo(in other);
    }

    public static bool operator <(
        in SurrealResult left,
        in SurrealResult right) {
        return left.CompareTo(in right) < 0;
    }

    public static bool operator <=(
        in SurrealResult left,
        in SurrealResult right) {
        return left.CompareTo(in right) <= 0;
    }

    public static bool operator >(
        in SurrealResult left,
        in SurrealResult right) {
        return left.CompareTo(in right) > 0;
    }

    public static bool operator >=(
        in SurrealResult left,
        in SurrealResult right) {
        return left.CompareTo(in right) >= 0;
    }


    [DoesNotReturn, DebuggerStepThrough,]
    private static int ThrowInvalidCompareTypes() {
        throw new InvalidOperationException("Cannot compare SurrealResult of different types, if one or more is not numeric..");
    }
}

/// <summary>
///     The result of a failed query to the Surreal database.
/// </summary>
public readonly struct SurrealError {
#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        SurrealError(
            int code,
            string? message) {
        Code = code;
        Message = message;
    }

    public int Code { get; }
    public string? Message { get; }
}

public sealed class SurrealAuthentication {
    [JsonPropertyName("ns")]
    public string? Namespace { get; set; }
    [JsonPropertyName("db")]
    public string? Database { get; set; }
    [JsonPropertyName("sc")]
    public string? Scope { get; set; }
    [JsonPropertyName("user")]
    public string? Username { get; set; }
    [JsonPropertyName("pass")]
    public string? Password { get; set; }
}
