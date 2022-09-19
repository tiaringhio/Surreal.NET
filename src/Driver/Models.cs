using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    private readonly int _split;
    public string Thing { get; }

    public ReadOnlySpan<char> Table => Thing.AsSpan(0, _split);
    public ReadOnlySpan<char> Key => _split == Length ? default : Thing.AsSpan(_split + 1);
    public int Length => Thing.Length;

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

    public static SurrealThing From(string? thing) {
        if (string.IsNullOrEmpty(thing)) {
            return default;
        }

        int split = thing.IndexOf(':');
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
        builder[table.Length] = ':';
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

    // Double implicit operators can result in problem, so we use explicit operators instead.
    public static explicit operator string(in SurrealThing thing) {
        return thing.Thing;
    }

    public sealed class Converter : JsonConverter<SurrealThing> {
        public override SurrealThing Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) {
            return reader.GetString();
        }

        public override SurrealThing ReadAsPropertyName(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) {
            return reader.GetString();
        }

        public override void Write(
            Utf8JsonWriter writer,
            SurrealThing value,
            JsonSerializerOptions options) {
            writer.WriteStringValue((string)value);
        }

        public override void WriteAsPropertyName(
            Utf8JsonWriter writer,
            SurrealThing value,
            JsonSerializerOptions options) {
            writer.WritePropertyName((string)value);
        }
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
        return (string)this;
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

    private static readonly JsonSerializerOptions _options = new() {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = false,
        // This was throwing an exception when set to JsonIgnoreCondition.Always
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IgnoreReadOnlyFields = false,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
    };

    /// <summary>
    /// Parses a <see cref="HttpResponseMessage"/> containing JSON to a <see cref="SurrealRestResponse"/>. 
    /// </summary>
    public static async Task<SurrealRestResponse> From(
        HttpResponseMessage msg,
        CancellationToken ct = default) {
        Stream stream = await msg.Content.ReadAsStreamAsync(ct);
        if (msg.StatusCode != HttpStatusCode.OK) {
            HttpError? err = await JsonSerializer.DeserializeAsync<HttpError>(stream, _options, ct);
            return From(err);
        }
        
        var successDocuments = await JsonSerializer.DeserializeAsync<List<HttpSuccess>>(stream, _options, ct);
        var successDocument = successDocuments?.FirstOrDefault(e => e.result.ValueKind != JsonValueKind.Null);

        if (await PeekIsEmpty(stream, ct)) {
            // Success and empty message -> invalid json
            return EmptyOk;
        }

        var successDocuments = await JsonSerializer.DeserializeAsync<List<HttpSuccess>>(stream, _options, ct);
        var successDocument = successDocuments?.FirstOrDefault(e => e.result.ValueKind != JsonValueKind.Null);

        return From(successDocument);
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
        return new(success?.time, success?.status, null, null, success.result);
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

        return new(rsp.Id, default, SurrealResult.From(rsp.Result));
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
    Document,
    None,
    String,
    SignedInteger,
    UnsignedInteger,
    Float,
    Boolean,
}

public struct SurrealStatus {
    public JsonElement Result { get; set; }
    public string Status { get; set; }
    public string Time { get; set; }
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

    public bool TryGetObject(out JsonElement document) {
        document = _json;
        return GetKind() == SurrealResultKind.Object;
    }
    
    public bool TryGetObjectCollection<T>([NotNullWhen(true)] out List<T>? document) {
        document = _json.Deserialize<List<T>>(Constants.JsonOptions);
        return document is not null;
    }

    public bool TryGetObject<T>([NotNullWhen(true)] out T? document) {
        if (_json.ValueKind == JsonValueKind.Array) {
            TryGetObjectCollection<T>(out var documents);
            document = documents.FirstOrDefault();
        } else {
            document = _json.Deserialize<T>(Constants.JsonOptions);
        }
        return document is not null;
    }

    public bool TryGetDocument(
        [NotNullWhen(true)] out string? id,
        out JsonElement document) {
        document = _json;
        bool isDoc = GetKind() == SurrealResultKind.Document;
        id = isDoc ? (string)_sentinelOrValue! : null;
        return isDoc;
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
    private static readonly object NoneSentinel = new();
    private static readonly object SignedIntegerSentinel = new();
    private static readonly object UnsignedIntegerSentinel = new();
    private static readonly object FloatSentinel = new();
    private static readonly object BooleanSentinel = new();

    private const long DocumentValue = 3;
    private const long TrueValue = 1;
    private const long FalseValue = 0;

    public static SurrealResult From(in JsonElement json) {
        return json.ValueKind switch {
            JsonValueKind.Undefined => new(json, NoneSentinel),
            JsonValueKind.Object => FromObject(json),
            JsonValueKind.Array => FromArray(json),
            JsonValueKind.String => new(json, json.GetString()),
            JsonValueKind.Number => FromNumber(json),
            JsonValueKind.True => new(json, BooleanSentinel, TrueValue),
            JsonValueKind.False => new(json, BooleanSentinel, FalseValue),
            JsonValueKind.Null => new(json, NoneSentinel),
            _ => ThrowUnknownJsonValueKind(json),
        };
    }

    public static SurrealResult FromArray(in JsonElement json) {
        // Try to unpack this document

        // Some results come as a simple array of objects (basically just the result array)
        // Others come embedded into a 'status document' that can have multiple result sets
        //[
        //  {
        //    "result": [ ... ],
        //    "status": "OK",
        //    "time": "71.775µs"
        //  }
        //]

        // First see if it the 'embeded status' document type, quick and dirty as a proof of concept
        List<SurrealStatus>? statusDocuments = json.Deserialize<List<SurrealStatus>>(Constants.JsonOptions);
        
        foreach (SurrealStatus statusDocument in statusDocuments) {
            if (string.IsNullOrEmpty(statusDocument.Status) && string.IsNullOrEmpty(statusDocument.Time)) {
                break; // This is not a status document and therefore must be a simple array of objects
            }

            // This probably is a status document
            JsonElement result = statusDocument.Result;

            if (result.ValueKind != JsonValueKind.Array) {
                // Skip over the statuses with no results
                continue;
            }

            return new(result, null);
        }
        
        return new(json, null);
    }

    private static SurrealResult FromObject(in JsonElement json) {
        if (json.ValueKind == JsonValueKind.String) {
            return new(json, json.GetString());
        }

        // A Document is requires the first property to be a string named "id".
        if (json.TryGetProperty("id", out JsonElement id) && id.ValueKind == JsonValueKind.String) {
            return new(json, id.GetString(), DocumentValue);
        }

        return new(json, null);
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
        if (ReferenceEquals(null, _sentinelOrValue)) {
            return SurrealResultKind.Object;
        }

        if (_sentinelOrValue is string) {
            return _int64ValueField == DocumentValue ? SurrealResultKind.Document : SurrealResultKind.String;
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
            // Documents are equal if the ids are equal, no matter the backing json value!
            SurrealResultKind.Document or SurrealResultKind.String =>
                string.Equals((string)_sentinelOrValue!, (string)other._sentinelOrValue!),
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
