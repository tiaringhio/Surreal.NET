using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;

using SurrealDB.Common;
using SurrealDB.Json;

namespace SurrealDB.Ws;

/// <summary>
///     The client used to connect to the Surreal server via JSON RPC.
/// </summary>
public sealed class WsClient : IDisposable, IAsyncDisposable {
    public const int DefaultBufferSize = 16 * 1024;

    // Do not get any funny ideas and fill this fucker up.
    public static readonly List<object?> EmptyList = new();

    private ClientWebSocket? _ws;

    /// <summary>
    ///     Indicates whether the client is connected or not.
    /// </summary>
    public bool Connected => _ws is not null && _ws.State == WebSocketState.Open;

    /// <summary>
    ///     Generates a random base64 string of the length specified.
    /// </summary>
    public static string GetRandomId(int length) {
        Span<byte> buf = stackalloc byte[length];
        Random.Shared.NextBytes(buf);
        return Convert.ToBase64String(buf);
    }

    /// <summary>
    ///     Opens the connection to the Surreal server.
    /// </summary>
    public async Task Open(
        Uri url,
        CancellationToken ct = default) {
        ThrowIfConnected();
        try {
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(url, ct);
        } catch {
            // Clean state
            _ws?.Dispose();
            _ws = null;
            throw;
        }
    }

    /// <summary>
    ///     Closes the connection to the Surreal server.
    /// </summary>
    public async Task Close(CancellationToken ct = default) {
        if (_ws is null) {
            return;
        }

        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", ct);
        _ws.Dispose();
        _ws = null;
    }

    /// <inheritdoc cref="IDisposable" />
    public void Dispose() {
        if (_ws is not null) {
            Close().Wait();
        }
    }

    /// <inheritdoc cref="IAsyncDisposable" />
    public ValueTask DisposeAsync() {
        return _ws is null ? default : new(Close());
    }

    /// <summary>
    ///     Sends the specified request to the Surreal server, and returns the response.
    /// </summary>
    /// <param name="req"> The request to send </param>
    public async Task<Response> Send(
        Request req,
        CancellationToken ct = default) {
        ThrowIfDisconnected();
        req.id ??= GetRandomId(6);
        req.parameters ??= EmptyList;

        await using PooledMemoryStream stream = new(DefaultBufferSize);
        
        await JsonSerializer.SerializeAsync(stream, req, Constants.JsonOptions, ct);
        await _ws!.SendAsync(stream.GetBehindBuffer(), WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, ct);
        stream.Position = 0;

        ValueWebSocketReceiveResult res;
        do {
            res = await _ws.ReceiveAsync(stream.InternalReadMemory(DefaultBufferSize), ct);
        } while (!res.EndOfMessage);

        // Swap from write to read mode
        long len = stream.Position - DefaultBufferSize + res.Count;
        stream.Position = 0;
        stream.SetLength(len);

        Response rsp = await JsonSerializer.DeserializeAsync<Response>(stream, Constants.JsonOptions, ct);
        return rsp;
    }

    private void ThrowIfDisconnected() {
        if (!Connected) {
            throw new InvalidOperationException("The connection is not open.");
        }
    }

    private void ThrowIfConnected() {
        if (Connected) {
            throw new InvalidOperationException("The connection is already open");
        }
    }
    
    public record struct Request(
        string? id,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
        bool async,
        string? method,
        [property: JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
        List<object?>? parameters);
    
    
    public readonly record struct Response(
        string? id,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
        Error error,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
        JsonElement result);
    
    public readonly record struct Error(
        int code,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
        string? message);
    
    
    public record struct Notify(
        string? id,
        string? method,
        [property: JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
        List<object?>? parameters);
    
    
}
