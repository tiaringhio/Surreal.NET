using System.Text.Json;

namespace SurrealDB.Models;

public readonly record struct OkOrErrorResult(
    string time,
    string status,
    string detail,
    JsonElement result) {
    public RawResult ToResult() => status.Equals("OK", StringComparison.OrdinalIgnoreCase)
        ? RawResult.Ok(time, status, detail, result)
        : RawResult.Error(time, status, detail, result);
}
