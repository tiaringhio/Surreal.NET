using System.Diagnostics.CodeAnalysis;

namespace SurrealDB.Models;

public interface IResponse {
    IEnumerable<OkResult> AllOkResults { get; }
    IEnumerable<ErrorResult> AllErrorResults { get; }

    bool HasErrors { get; }
    bool IsEmpty { get; }

    public IReadOnlyList<IResult> Results { get; }

    public bool TryGetFirstErrorResult(out ErrorResult errorResult);
    public static bool TryGetFirstErrorResult(IResponse response, out ErrorResult errorResult) {
        foreach (var result in response.Results) {
            if (result is ErrorResult e) {
                errorResult = e;
                return true;
            }
        }
        errorResult = default;
        return false;
    }

    public bool TryGetFirstOkResult(out OkResult okResult);
    public static bool TryGetFirstOkResult(IResponse response, out OkResult okResult) {
        foreach (var result in response.Results) {
            if (result is OkResult o) {
                okResult = o;
                return true;
            }
        }
        okResult = default;
        return false;
    }
}
