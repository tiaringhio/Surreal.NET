using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Surreal.NET;

#if SURREAL_NET_INTERNAL
    public
#endif
sealed class RpcClient : IDisposable
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
        await JsonSerializer.SerializeAsync(stream, req, SourceGenerationContext.Default.RpcRequest, ct);
        
        await stream.FlushAsync(ct);

        var rsp = await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.RpcResponse, ct);
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

#if SURREAL_NET_INTERNAL
    public
#endif
struct RpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] 
    public string? Message { get; set; }
    
    
}

#if SURREAL_NET_INTERNAL
    public
#endif
struct RpcRequest
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("async"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Async { get; set; }
    
    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IList<object?>? Params { get; set; }
}


#if SURREAL_NET_INTERNAL
    public
#endif
struct RpcResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("error"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public RpcError? Error { get; set; }

    [JsonPropertyName("result"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonDocument? Result { get; set; }
}

#if SURREAL_NET_INTERNAL
    public
#endif
struct RpcNotification
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("method")]
    public string Method { get; set; }

    [JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IList<object?>? Params { get; set; }
}

[JsonSerializable(typeof(RpcError))]
[JsonSerializable(typeof(RpcRequest))]
[JsonSerializable(typeof(RpcResponse))]
[JsonSerializable(typeof(RpcNotification))]

#if SURREAL_NET_INTERNAL
    public
#endif
partial class SourceGenerationContext : JsonSerializerContext
{
}