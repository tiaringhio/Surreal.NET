using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Surreal.Net;

/// <summary>
/// Indicates a table or a specific record.
/// </summary>
/// <remarks>
/// `table_name:record_id`
/// </remarks>
public readonly struct SurrealThing
{
    private readonly int _split;
    public string Thing { get; }

    public ReadOnlySpan<char> Table => Thing.AsSpan(0, _split);
    public ReadOnlySpan<char> Key => Thing.AsSpan(_split + 1);
    public int Length => Thing.Length;

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
    SurrealThing(int split, string thing)
    {
        _split = split;
        Thing = thing;
    }

    public override string ToString() => Thing;

    public static SurrealThing From(string thing) => new(thing.IndexOf(':'), thing);

    public static SurrealThing From(in ReadOnlySpan<char> table, in ReadOnlySpan<char> key) =>
        new(table.Length, $"{table}:{key}");

    public static implicit operator SurrealThing(in string thing) => From(thing);

    public SurrealThing WithTable(in ReadOnlySpan<char> table)
    {
        int keyOffset = table.Length + 1;
        int chars = keyOffset + Key.Length;
        Span<char> builder = stackalloc char[chars];
        table.CopyTo(builder);
        builder[table.Length] = ':';
        Key.CopyTo(builder.Slice(keyOffset));
        return new(table.Length, builder.ToString());
    }

    public SurrealThing WithKey(in ReadOnlySpan<char> key)
    {
        int keyOffset = Table.Length + 1;
        int chars = keyOffset + key.Length;
        Span<char> builder = stackalloc char[chars];
        Table.CopyTo(builder);
        builder[Table.Length] = ':';
        key.CopyTo(builder.Slice(keyOffset));
        return new(Table.Length, builder.ToString());
    }

    public static implicit operator string(in SurrealThing thing) => thing.Thing;
}

public interface ISurrealResponse
{
    public bool IsOk { get; }

    public bool IsError { get; }

    public bool TryGetError(out SurrealError error);

    public bool TryGetResult(out SurrealResult result);

    public bool TryGetResult(out SurrealResult result, out SurrealError error);
}

/// <summary>
/// The response from a query to the Surreal database via rest.
/// </summary>
public readonly struct SurrealRestResponse : ISurrealResponse
{
    private readonly string? _time;
    private readonly string? _status;
    private readonly string? _detail;
    private readonly string? _description;
    private readonly JsonElement _result;

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
    SurrealRestResponse(string? time, string? status, string? description, string? detail, JsonElement result)
    {
        _time = time;
        _status = status;
        _detail = detail;
        _result = result;
        _description = description;
    }

    public string? Time => _time;

    public bool IsOk => String.Equals(_status, "ok", StringComparison.OrdinalIgnoreCase);

    public bool IsError => !IsOk;

    public bool TryGetError(out SurrealError error)
    {
        if (IsOk)
        {
            error = default;
            return false;
        }

        error = new(1, $"{_detail}: {_description}");
        return true;
    }

    public bool TryGetResult(out SurrealResult result)
    {
        if (IsError)
        {
            result = default;
            return false;
        }

        result = SurrealResult.From(_result);
        return true;
    }

    public bool TryGetResult(out SurrealResult result, out SurrealError error)
    {
        if (IsError)
        {
            result = default;
            error = new(1, _detail);
            ;
            return false;
        }

        result = SurrealResult.From(_result);
        error = default;
        return true;
    }

    private static JsonSerializerOptions _options = new()
    {
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

    public static async Task<SurrealRestResponse> From(HttpResponseMessage msg)
    {
        if (msg.StatusCode != HttpStatusCode.OK)
        {
            return new(null, "error", msg.ReasonPhrase, null, default);
        }

        var stream = await msg.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<SurrealRestResponse>(stream, _options);
    }
}
public static class SurrealRestClientExtensions
{
    [DebuggerStepThrough]
    public static Task<SurrealRestResponse> ToSurreal(this HttpResponseMessage msg) => SurrealRestResponse.From(msg);
}

/// <summary>
/// The response from a query to the Surreal database via rpc.
/// </summary>
public readonly struct SurrealRpcResponse : ISurrealResponse
{
    private readonly string _id;
    private readonly SurrealResult _result;
    private readonly SurrealError _error;

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
    SurrealRpcResponse(string id, SurrealError error, SurrealResult result)
    {
        _id = id;
        _error = error;
        _result = result;
    }

    public string Id => _id;

    public bool IsOk => _error.Code == 0;
    public bool IsError => _error.Code != 0;

    public SurrealResult UncheckedResult => _result;
    public SurrealError UncheckedError => _error;

    public bool TryGetError(out SurrealError error)
    {
        error = _error;
        return IsError;
    }

    public bool TryGetResult(out SurrealResult result)
    {
        result = _result;
        return IsOk;
    }

    public bool TryGetResult(out SurrealResult result, out SurrealError error)
    {
        result = _result;
        error = _error;
        return IsOk;
    }

    public void Deconstruct(out SurrealResult result, out SurrealError error) => (result, error) = (_result, _error);

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
    static SurrealRpcResponse From(in RpcResponse rsp)
    {
        if (rsp.Id is null)
        {
            ThrowIdMissing();
        }

        if (rsp.Error.HasValue)
        {
            var err = rsp.Error.Value;
            return new(rsp.Id, new(err.Code, err.Message), default);
        }

        return new(rsp.Id, default, SurrealResult.From(rsp.Result));
    }

    [DoesNotReturn]
    private static void ThrowIdMissing()
    {
        throw new InvalidOperationException("Response does not have an id.");
    }
}

public static class SurrealRpcClientExtensions
{
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

public enum SurrealResultKind : byte
{
    Object,
    Document,
    None,
    String,
    SignedInteger,
    UnsignedInteger,
    Float,
    Boolean,
}

/// <summary>
/// The result of a successful query to the Surreal database.
/// </summary>
public readonly struct SurrealResult : IEquatable<SurrealResult>, IComparable<SurrealResult>
{
    private readonly JsonElement _json;
    private readonly object? _sentinelOrValue;
    private readonly long _int64ValueField;

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
    SurrealResult(JsonElement json, object? sentinelOrValue)
    {
        _json = json;
        _sentinelOrValue = sentinelOrValue;
        _int64ValueField = 0;
    }

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
    SurrealResult(JsonElement json, object? sentinelOrValue, long int64ValueField)
    {
        _json = json;
        _sentinelOrValue = sentinelOrValue;
        _int64ValueField = int64ValueField;
    }

    public JsonElement Inner => _json;

    public bool TryGetObject(out JsonElement document)
    {
        document = _json;
        return GetKind() == SurrealResultKind.Object;
    }

    public bool TryGetDocument(out string? id, out JsonElement document)
    {
        document = _json;
        bool isDoc = GetKind() == SurrealResultKind.Document;
        id = isDoc ? (string)_sentinelOrValue! : null;
        return isDoc;
    }

    public bool TryGetValue(out string? value)
    {
        bool isString = GetKind() == SurrealResultKind.String;
        value = isString ? (string)_sentinelOrValue! : null;
        return isString;
    }

    public bool TryGetValue(out long value)
    {
        bool isInt = GetKind() == SurrealResultKind.SignedInteger;
        value = isInt ? _int64ValueField : 0;
        return isInt;
    }

    public bool TryGetValue(out ulong value)
    {
        bool isInt = GetKind() == SurrealResultKind.SignedInteger;
        long data = _int64ValueField;
        value = isInt ? Unsafe.As<long, ulong>(ref data) : 0;
        return isInt;
    }

    public bool TryGetValue(out double value)
    {
        bool isFloat = GetKind() == SurrealResultKind.Float;
        long data = _int64ValueField;
        value = isFloat ? Unsafe.As<long, double>(ref data) : 0;
        return isFloat;
    }

    public bool TryGetValue(out bool value)
    {
        bool isBoolean = GetKind() == SurrealResultKind.Boolean;
        value = isBoolean && _int64ValueField != FalseValue;
        return value;
    }

    // Below is the logic determining the type of the boxed value in the result.
    // The type is primarily determined by the presence of a sentinel.
    // Both strings and documents make use of the sentinel field as a value field,
    // In this case the valueField determines the type.
    private static object NoneSentinel = new();
    private static object SignedIntegerSentinel = new();
    private static object UnsignedIntegerSentinel = new();
    private static object FloatSentinel = new();
    private static object BooleanSentinel = new();

    private const long DocumentValue = 3;
    private const long TrueValue = 1;
    private const long FalseValue = 0;

    public static SurrealResult From(in JsonElement json)
    {
        return json.ValueKind switch
        {
            JsonValueKind.Undefined => new(json, NoneSentinel),
            JsonValueKind.Object => FromObject(json),
            JsonValueKind.Array => new(json, null),
            JsonValueKind.String => new(json, json.GetString()),
            JsonValueKind.Number => FromNumber(json),
            JsonValueKind.True => new(json, BooleanSentinel, TrueValue),
            JsonValueKind.False => new(json, BooleanSentinel, FalseValue),
            JsonValueKind.Null => new(json, NoneSentinel),
            _ => ThrowUnknownJsonValueKind(json)
        };
    }

    private static SurrealResult FromObject(in JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.String)
        {
            return new(json, json.GetString());
        }

        // A Document is requires the first property to be a string named "id".
        if (json.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
        {
            return new(json, id.GetString(), DocumentValue);
        }

        return new(json, null);
    }

    private static SurrealResult FromNumber(in JsonElement json)
    {
        if (json.TryGetInt64(out long signed))
        {
            return new(json, SignedIntegerSentinel, signed);
        }

        if (json.TryGetUInt64(out ulong unsigned))
        {
            return new(json, UnsignedIntegerSentinel, Unsafe.As<ulong, long>(ref unsigned));
        }

        if (json.TryGetDouble(out double dbl))
        {
            return new(json, FloatSentinel, Unsafe.As<double, long>(ref dbl));
        }

        return new(json, NoneSentinel);
    }

    private SurrealResultKind GetKind()
    {
        if (Object.ReferenceEquals(null, _sentinelOrValue))
        {
            return SurrealResultKind.Object;
        }

        if (_sentinelOrValue is string)
        {
            return _int64ValueField == DocumentValue ? SurrealResultKind.Document : SurrealResultKind.String;
        }

        if (Object.ReferenceEquals(NoneSentinel, _sentinelOrValue))
        {
            return SurrealResultKind.None;
        }

        if (Object.ReferenceEquals(SignedIntegerSentinel, _sentinelOrValue))
        {
            return SurrealResultKind.SignedInteger;
        }

        if (Object.ReferenceEquals(UnsignedIntegerSentinel, _sentinelOrValue))
        {
            return SurrealResultKind.UnsignedInteger;
        }

        if (Object.ReferenceEquals(FloatSentinel, _sentinelOrValue))
        {
            return SurrealResultKind.Float;
        }

        if (Object.ReferenceEquals(BooleanSentinel, _sentinelOrValue))
        {
            return SurrealResultKind.Boolean;
        }

        Debug.Assert(false); // Should not happen, but is not fatal; None covers all edge cases.
        return SurrealResultKind.None;
    }

    [DoesNotReturn, DebuggerStepThrough]
    private static SurrealResult ThrowUnknownJsonValueKind(JsonElement json)
    {
        throw new ArgumentOutOfRangeException(nameof(json), json.ValueKind, "Unknown value kind.");
    }

    // Below is the implementation for the comparison and equality logic,
    // as well as operator overloads and conversion logic for IConvertible.

    public bool Equals(in SurrealResult other)
    {
        // Fastest check for inequality, is via the value field.
        if (_int64ValueField != other._int64ValueField)
        {
            return false;
        }

        // More expensive check for the type of the boxed value.
        SurrealResultKind kind = GetKind();

        // Most expensive check requires unboxing of the value.
        return kind == other.GetKind() && EqualsUnboxed(in other, in kind);
    }

    private bool EqualsUnboxed(in SurrealResult other, in SurrealResultKind kind)
    {
        return kind switch
        {
            SurrealResultKind.Object or SurrealResultKind.None => EqualityComparer<JsonElement>.Default.Equals(_json,
                other._json),
            // Documents are equal if the ids are equal, no matter the backing json value!
            SurrealResultKind.Document or SurrealResultKind.String =>
                String.Equals((string)_sentinelOrValue!, (string)other._sentinelOrValue!),
            // Due to the unsafe case we are still able to use the operator and do not need to cast to compare structs.
            _ => _int64ValueField == other._int64ValueField,
        };
    }

    // The struct is big, do not copy if not necessary!
    bool IEquatable<SurrealResult>.Equals(SurrealResult other) => Equals(in other);

    public override bool Equals(object? obj)
    {
        return obj is SurrealResult other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_json.ValueKind, _sentinelOrValue, _int64ValueField);
    }

    public static bool operator ==(in SurrealResult left, in SurrealResult right) => left.Equals(in right);
    public static bool operator !=(in SurrealResult left, in SurrealResult right) => !left.Equals(in right);


    public int CompareTo(in SurrealResult other)
    {
        SurrealResultKind thisKind = GetKind();
        SurrealResultKind otherKind = other.GetKind();

        long thisValue = _int64ValueField;
        long otherValue = other._int64ValueField;

        return (thisKind, otherKind) switch
        {
            (SurrealResultKind.SignedInteger, SurrealResultKind.SignedInteger) => thisValue.CompareTo(otherValue),
            (SurrealResultKind.SignedInteger, SurrealResultKind.UnsignedInteger) =>
                ((double)thisValue).CompareTo((double)Unsafe.As<long, ulong>(ref otherValue)),
            (SurrealResultKind.SignedInteger, SurrealResultKind.Float) =>
                ((double)thisValue).CompareTo(Unsafe.As<long, double>(ref otherValue)),

            (SurrealResultKind.UnsignedInteger, SurrealResultKind.SignedInteger) =>
                ((double)Unsafe.As<long, ulong>(ref thisValue)).CompareTo((double)otherValue),
            (SurrealResultKind.UnsignedInteger, SurrealResultKind.UnsignedInteger) =>
                Unsafe.As<long, ulong>(ref thisValue).CompareTo(Unsafe.As<long, ulong>(ref otherValue)),
            (SurrealResultKind.UnsignedInteger, SurrealResultKind.Float) =>
                ((double)Unsafe.As<long, ulong>(ref thisValue)).CompareTo(Unsafe.As<long, double>(ref otherValue)),

            (SurrealResultKind.Float, SurrealResultKind.SignedInteger) =>
                Unsafe.As<long, double>(ref thisValue).CompareTo((double)otherValue),
            (SurrealResultKind.Float, SurrealResultKind.UnsignedInteger) =>
                Unsafe.As<long, double>(ref thisValue).CompareTo((double)Unsafe.As<long, ulong>(ref otherValue)),
            (SurrealResultKind.Float, SurrealResultKind.Float) =>
                Unsafe.As<long, double>(ref thisValue).CompareTo(Unsafe.As<long, double>(ref otherValue)),

            _ => ThrowInvalidCompareTypes(),
        };
    }

    // The struct is big, do not copy if not necessary!
    int IComparable<SurrealResult>.CompareTo(SurrealResult other) => CompareTo(in other);

    public static bool operator <(in SurrealResult left, in SurrealResult right) => left.CompareTo(in right) < 0;
    public static bool operator <=(in SurrealResult left, in SurrealResult right) => left.CompareTo(in right) <= 0;
    public static bool operator >(in SurrealResult left, in SurrealResult right) => left.CompareTo(in right) > 0;
    public static bool operator >=(in SurrealResult left, in SurrealResult right) => left.CompareTo(in right) >= 0;


    [DoesNotReturn, DebuggerStepThrough]
    private static int ThrowInvalidCompareTypes()
    {
        throw new InvalidOperationException(
            "Cannot compare SurrealResult of different types, if one or more is not numeric..");
    }
}

/// <summary>
/// The result of a failed query to the Surreal database.
/// </summary>
public readonly struct SurrealError
{
#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
    SurrealError(int code, string? message)
    {
        Code = code;
        Message = message;
    }

    public int Code { get; }
    public string? Message { get; }
}

public sealed class SurrealAuthentication
{
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