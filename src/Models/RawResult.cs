using System.Text.Json;

using SurrealDB.Common;

namespace SurrealDB.Models;

public readonly record struct RawResult(string time,
    string status,
    string detail,
    JsonElement result) {
    public IResult ToResult() {
        if (status == OK) {
            return OkResult.From(result.IntoSingle());
        } else {
            return new ErrorResult(-1, status, detail);
        }
    }

    public const string OK = "OK";
}
