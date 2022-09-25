using System.Diagnostics;

namespace SurrealDB.Models;

/// <summary>
///     The result of a failed query to the Surreal database.
/// </summary>
[DebuggerDisplay("{Code},nq: {Message},nq")]
public readonly struct Error {
    public Error(
            int code,
            string? message) {
        Code = code;
        Message = message;
    }

    public int Code { get; }
    public string? Message { get; }
}
