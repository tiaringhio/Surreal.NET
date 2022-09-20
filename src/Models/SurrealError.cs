namespace SurrealDB.Models;

/// <summary>
///     The result of a failed query to the Surreal database.
/// </summary>
public readonly struct SurrealError {
    public SurrealError(
            int code,
            string? message) {
        Code = code;
        Message = message;
    }

    public int Code { get; }
    public string? Message { get; }
}