namespace SurrealDB.Models;

public interface IResponse {
    public bool IsOk { get; }

    public bool IsError { get; }

    public bool TryGetError(out Error error);

    public bool TryGetResult(out Result result);

    public bool TryGetResult(
        out Result result,
        out Error error);
}
