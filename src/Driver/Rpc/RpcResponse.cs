using System.Diagnostics;

using SurrealDB.Models;

namespace SurrealDB.Driver.Rpc;

/// <summary>
///     The response from a query to the Surreal database via RPC.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct RpcResponse : IResponse {
    internal static RpcResponse EmptyOk = new ();

    public IReadOnlyList<IResult> Results { get; }

    public RpcResponse() {
        Results = new List<IResult>();
    }
    public RpcResponse(List<IResult> results) {
        Results = results;
    }
    public RpcResponse(IResult result) {
        Results = new List<IResult> { result };
    }

    public IEnumerable<OkResult> AllOkResults => Results.OfType<OkResult>();
    public IEnumerable<ErrorResult> AllErrorResults => Results.OfType<ErrorResult>();
    public bool HasErrors => AllErrorResults.Any();
    public bool IsEmpty => !Results.Any();

    public bool TryGetFirstErrorResult(out ErrorResult errorResult) {
        return IResponse.TryGetFirstErrorResult(this, out errorResult);
    }

    public bool TryGetFirstOkResult(out OkResult okResult) {
        return IResponse.TryGetFirstOkResult(this, out okResult);
    }
}
