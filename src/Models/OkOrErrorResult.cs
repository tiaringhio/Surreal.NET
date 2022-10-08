using System.Text.Json;

namespace SurrealDB.Models;

public readonly record struct OkOrErrorResult(
    string Time,
    string Status,
    string Detail,
    JsonElement Result) {
    public RawResult ToResult() => Status.Equals("OK", StringComparison.OrdinalIgnoreCase)
        ? RawResult.Ok(Time, Status, Detail, Result)
        : RawResult.Error(Time, Status, Detail, Result);
}
