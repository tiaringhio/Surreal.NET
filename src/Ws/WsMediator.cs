using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using SurrealDB.Ws.Models;

namespace SurrealDB.Ws;

public sealed class WsMediator : IDisposable, IAsyncDisposable {
    private readonly CancellationTokenSource _cts = new();
    private readonly WsChannel _channel = new();
    private readonly ConcurrentDictionary<string, IHandler> _handlers = new();
    private readonly Task _recv;

    public WsMediator() {
        _recv = Receive(_cts.Token);
    }

    /// <summary>
    /// Sends the request and awaits a response from the server
    /// </summary>
    public async Task<(ResponseHeader rsp, NotifyHeader nty, Stream stm)> RequestOnce(string id, Stream request, CancellationToken ct = default) {
        ResponseHandler handler = new(id, ct);
        Register(handler);
        await _channel.Reqt(request, ct);
        return await handler.Task;
    }

    /// <summary>
    /// Sends the request and awaits responses from the server until manually canceled using the cancellation token
    /// </summary>
    public async IAsyncEnumerable<(ResponseHeader rsp, NotifyHeader nty, Stream stm)> RequestPersits(string id, Stream request, [EnumeratorCancellation] CancellationToken ct = default) {
        NotificationHandler handler = new(this, id, ct);
        Register(handler);
        await _channel.Reqt(request, ct);
        await foreach (var res in handler) {
            yield return res;
        }
    }

    internal void Register(IHandler handler) {
        if (!_handlers.TryAdd(handler.Id, handler)) {
            ThrowDuplicateId(handler.Id);
        }
    }

    internal void Unregister(IHandler handler) {
        if (!_handlers.TryRemove(handler.Id, out var h)) {
            return;
        }

        try {
            h.Dispose();
        } catch (OperationCanceledException) {
            // expected
        }
    }

    private async Task Receive(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            var (id, response, notify, stream) = await _channel.Recv(stoppingToken);

            stoppingToken.ThrowIfCancellationRequested();

            if (!_handlers.TryGetValue(id, out IHandler handler)) {
                // assume that unhandled responses belong to other clients
                // discard!
                await stream.DisposeAsync();
                continue;
            }
            if (!handler.Persistent) {
                // persistent handlers are for notifications and are not removed automatically
                Unregister(handler);
            }

            handler.Handle(response, notify, stream);
        }
    }

    public async Task Cancel() {
        foreach (var handler in _handlers.Values) {
            Unregister(handler);
        }
        try {
            _cts.Cancel();
            await _recv;
        } catch (OperationCanceledException) {
            // expected
        }
    }

    public void Dispose() {
        Cancel().Wait();
        _channel.Dispose();
    }

    public async ValueTask DisposeAsync() {
        await Cancel();
        _channel.Dispose();
    }

    [DoesNotReturn]
    private static void ThrowDuplicateId(string id) {
        throw new ArgumentOutOfRangeException(nameof(id), $"A request with the Id `{id}` is already registered");
    }
}
