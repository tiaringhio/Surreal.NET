using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Surreal.NET;

internal sealed class RpcClient : IDisposable
{
    private TcpClient? _ws;

    public bool Connected => _ws is not null && _ws.Connected;

    public async Task Open(SurrealConfig config, CancellationToken ct = default)
    {
        config.ThrowIfInvalid();
        ThrowIfConnected();
        
        _ws = new TcpClient();
        try
        {
            await _ws.ConnectAsync(config.Remote!, ct);
        }
        catch
        {
            // Clean state
            _ws.Dispose();
            _ws = null;
            throw;
        }
    }

    public void Close()
    {
        _ws?.Close();
    }

    public async Task<RpcResponse> Send(RpcRequest req, CancellationToken ct = default)
    {
        ThrowIfDisconnected();
        string id = Id.GetRandom(16);
        NetworkStream stream = _ws!.GetStream();
        await JsonSerializer.SerializeAsync(stream, req, typeof(RpcRequest), SourceGenerationContext.Default, ct);
        
        await stream.FlushAsync(ct);

        var rsp = await JsonSerializer.DeserializeAsync<RpcResponse>(stream, typeof(RpcResponse), SourceGenerationContext.Default, ct);
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

    public void Dispose()
    {
        if (_ws is not null)
        {
            Close();
            _ws.Dispose();
        }
    }
}

internal struct RpcError
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; }
}

internal struct RpcRequest
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("async")] public bool Async { get; set; }
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("params")] public IList<object?> Params { get; set; }
}


internal struct RpcResponse
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("error")] public RpcError? Error { get; set; }
    [JsonPropertyName("result")] public JsonDocument Result { get; set; }
}

internal struct RpcNotification
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("params")] public IList<object?> Params { get; set; }
}

[JsonSerializable(typeof(RpcError))]
[JsonSerializable(typeof(RpcRequest))]
[JsonSerializable(typeof(RpcResponse))]
[JsonSerializable(typeof(RpcNotification))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}