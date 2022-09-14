using System.Net.WebSockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Surreal.Net;

/// <summary>
/// The client used to connect to the Surreal server via JSON RPC.
/// </summary>
#if SURREAL_NET_INTERNAL
public
#endif
    sealed class JsonRpcClient : IDisposable, IAsyncDisposable
{
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        DictionaryKeyPolicy = JsonLowerSnakeCaseNamingPolicy.Instance,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // TODO: Remove this when the server is fixed, see: https://github.com/surrealdb/surrealdb/issues/137
        NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonLowerSnakeCaseNamingPolicy.Instance,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement,
        WriteIndented = false
    };
    
    public const int DefaultBufferSize = 16 * 1024;

    // Do not get any funny ideas and fill this fucker up.
    public static readonly List<object?> EmptyList = new();
    
    private ClientWebSocket? _ws;

    /// <summary>
    /// Indicates whether the client is connected or not.
    /// </summary>
    public bool Connected => _ws is not null && _ws.State == WebSocketState.Open;

    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used for serialization.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; } = DefaultJsonSerializerOptions;

    /// <summary>
    /// Generates a random base64 string of the length specified.
    /// </summary>
    public static string GetRandomId(int length)
    {
        Span<byte> buf = stackalloc byte[length];
        Random.Shared.NextBytes(buf);
        return Convert.ToHexString(buf);
    }

    /// <summary>
    /// Opens the connection to the Surreal server.
    /// </summary>
    public async Task Open(Uri url, CancellationToken ct = default)
    {
        ThrowIfConnected();
        try
        {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(url, ct);
        }
        catch
        {
            // Clean state
            _ws?.Dispose();
            _ws = null;
            throw;
        }
    }

    /// <summary>
    /// Closes the connection to the Surreal server.
    /// </summary>
    public async Task Close(CancellationToken ct = default)
    {
        if (_ws is null)
        {
            return;
        }

        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", ct);
        _ws.Dispose();
        _ws = null;
    }

    /// <inheritdoc cref="IDisposable"/>
    public void Dispose()
    {
        if (_ws is not null)
        {
            Close().Wait();
        }
    }

    /// <inheritdoc cref="IAsyncDisposable"/>
    public ValueTask DisposeAsync()
    {
        return _ws is null ? default : new(Close());
    }

    /// <summary>
    /// Sends the specified request to the Surreal server, and returns the response.
    /// </summary>
    /// <param name="req">The request to send</param>
    public async Task<RpcResponse> Send(RpcRequest req, CancellationToken ct = default)
    {
        ThrowIfDisconnected();
        req.Id ??= GetRandomId(6);
        req.Params ??= EmptyList;

        await using PooledMemoryStream stream = new(DefaultBufferSize);
        
        await JsonSerializer.SerializeAsync(stream, req, SerializerOptions, ct);
        await _ws!.SendAsync(stream.GetConsumedBuffer(), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, ct);
        stream.Position = 0;
        
        ValueWebSocketReceiveResult res;
        do
        {
            res = await _ws.ReceiveAsync(stream.InternalReadMemory(DefaultBufferSize), ct);
        } while (!res.EndOfMessage);
        
        // Swap from write to read mode
        long len = stream.Position - DefaultBufferSize + res.Count;
        stream.Position = 0;
        stream.SetLength(len);
        
        var rsp = await JsonSerializer.DeserializeAsync<RpcResponse>(stream, SerializerOptions, ct);
        return rsp;
    }

    private void ThrowIfDisconnected()
    {
        if (!Connected)
        {
            throw new InvalidOperationException("The connection is not open.");
        }
    }

    private void ThrowIfConnected()
    {
        if (Connected)
        {
            throw new InvalidOperationException("The connection is already open");
        }
    }
}

#if SURREAL_NET_INTERNAL
public
#endif
    struct RpcError
{
    [JsonPropertyName("code")] public int Code { get; set; }

    [JsonPropertyName("message"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Message { get; set; }
}

#if SURREAL_NET_INTERNAL
public
#endif
    struct RpcRequest
{
    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("async"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Async { get; set; }

    [JsonPropertyName("method")] public string? Method { get; set; }

    [JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<object?> Params { get; set; }
}


#if SURREAL_NET_INTERNAL
public
#endif
    struct RpcResponse
{
    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("error"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public RpcError? Error { get; set; }

    [JsonPropertyName("result"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonElement Result { get; set; }
}

#if SURREAL_NET_INTERNAL
public
#endif
    struct RpcNotification
{
    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("method")] public string? Method { get; set; }

    [JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<object?>? Params { get; set; }
}

// // Serialization for rpc messages
// [JsonSerializable(typeof(RpcError))]
// [JsonSerializable(typeof(RpcRequest))]
// [JsonSerializable(typeof(RpcResponse))]
// [JsonSerializable(typeof(RpcNotification))]
// // Serialization for dependent types 
// [JsonSerializable(typeof(bool))]
// [JsonSerializable(typeof(string))]
// [JsonSerializable(typeof(Dictionary<string, object?>))]
// [JsonSerializable(typeof(object))]
// #if SURREAL_NET_INTERNAL
//     public
// #endif
// partial class SourceGenerationContext : JsonSerializerContext
// {
// }