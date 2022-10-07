using System.Diagnostics;

using SurrealDB.Models;

namespace SurrealDB.Driver.Rest;

/// <summary>
///     The response from a query to the Surreal database via REST.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct RestResponse : IResponse {
    internal static RestResponse EmptyOk = new ();

    public IReadOnlyList<IResult> Results { get; }

    public RestResponse() {
        Results = new List<IResult>();
    }
    public RestResponse(List<IResult> results) {
        Results = results;
    }
    public RestResponse(IResult result) {
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
