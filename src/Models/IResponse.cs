using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

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

    /// <summary>
    /// Returns the first non null <see cref="OkResult"/> in the result set
    /// </summary>
    public bool TryGetFirstOkResult(out OkResult okResult);
    public static bool TryGetFirstOkResult(IResponse response, out OkResult okResult) {
        foreach (var result in response.Results) {
            if (result is OkResult o) {
                if (o.Inner.ValueKind == JsonValueKind.Null) {
                    continue;
                }

                okResult = o;
                return true;
            }
        }
        okResult = default;
        return false;
    }
}
