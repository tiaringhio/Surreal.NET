namespace SurrealDB.Ws.Models;

internal interface IHandler : IDisposable {

    public string Id { get; }

    public bool Persistent { get; }

    public void Handle(ResponseHeader rsp, NotifyHeader nty, Stream stm);
}

internal sealed class ResponseHandler : IHandler {
    private TaskCompletionSource<(ResponseHeader, NotifyHeader, Stream)>? _tcs = new();
    private readonly string _id;
    private readonly CancellationToken _ct;

    public ResponseHandler(string id, CancellationToken ct) {
        _id = id;
        _ct = ct;
    }

    public Task<(ResponseHeader rsp, NotifyHeader nty, Stream stm)> Task => _tcs!.Task;

    public string Id => _id;

    public bool Persistent => false;

    public void Handle(ResponseHeader rsp, NotifyHeader nty, Stream stm) {
        _tcs?.SetResult((rsp, nty, stm));
        _tcs = null;
    }

    public void Dispose() {
        _tcs?.SetCanceled();
    }

}

internal class NotificationHandler : IHandler, IAsyncEnumerable<(ResponseHeader rsp, NotifyHeader nty, Stream stm)> {
    private readonly WsMediator _mediator;
    private readonly CancellationToken _ct;
    private TaskCompletionSource<(ResponseHeader, NotifyHeader, Stream)> _tcs = new();
    public NotificationHandler(WsMediator mediator, string id, CancellationToken ct) {
        _mediator = mediator;
        Id = id;
        _ct = ct;
    }

    public string Id { get; }
    public bool Persistent => true;

    public void Handle(ResponseHeader rsp, NotifyHeader nty, Stream stm) {
        _tcs.SetResult((rsp, nty, stm));
        _tcs = new();
    }

    public void Dispose() {
        _tcs.SetCanceled();
    }

    public async IAsyncEnumerator<(ResponseHeader rsp, NotifyHeader nty, Stream stm)> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
        while (!_ct.IsCancellationRequested) {
            (ResponseHeader, NotifyHeader, Stream) res;
            try {
                res = await _tcs.Task;
            } catch (OperationCanceledException) {
                // expected on remove
                yield break;
            }
            yield return res;
        }

        // unregister before throwing
        if (_ct.IsCancellationRequested) {
            _mediator.Unregister(this);
        }
        _ct.ThrowIfCancellationRequested();
    }
}

