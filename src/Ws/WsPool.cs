using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text.Json;

using SurrealDB.Common;

namespace SurrealDB.Ws;

public sealed class WsPool : IDisposable {
    private readonly ClientWebSocket _ws = new();

    public static int DefaultBufferSize => 16 * 1024;

    /// <summary>
    ///     Indicates whether the client is connected or not.
    /// </summary>
    public bool Connected => _ws.State == WebSocketState.Open;

    public async Task Open(Uri remote, CancellationToken ct = default) {
        ThrowIfConnected();
        await _ws.ConnectAsync(remote, ct);
    }

    public async Task Close(CancellationToken ct = default) {
        if (_ws.State == WebSocketState.Closed) {
            return;
        }
        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "client disconnect", ct);
    }

    private async Task<(ReqHead head, Stream body)> Recv(CancellationToken ct) {
        IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(DefaultBufferSize);
        var r = await _ws.ReceiveAsync(owner.Memory, ct);
        // parse the head
        var (head, off, err) = ReqHead.Parse(owner.Memory.Span.Slice(0, r.Count));
        if (err is not null) {
            ThrowParseHead(err, off);
        }

        Stream body = GetStream(r, owner, head);
        return (head, body);
    }

    private Stream GetStream(ValueWebSocketReceiveResult r, IMemoryOwner<byte> owner, ReqHead head) {
        // check if rsp is completely in the page
        if (r.EndOfMessage) {
            // create a owned stream from the remainder.
            return RentedMemoryStream.FromMemory(owner, owner.Memory.Slice(r.Count), true, true);
        }

        // in this case the response doesnt fit inside the buffer.
        // we create a stream wrapping the websocket
        return new WsStream(owner, owner.Memory, _ws);
    }

    public void Dispose() {
        _ws.Dispose();
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

    [DoesNotReturn]
    private static void ThrowParseHead(string err, int off) {
        throw new JsonException(err, default, default, off);
    }
}
