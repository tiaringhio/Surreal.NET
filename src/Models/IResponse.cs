namespace SurrealDB.Models;

public interface IResponse {
    public bool IsOk { get; }

    public bool IsError { get; }

    public bool TryGetError(out SurrealError error);

    public bool TryGetResult(out Result result);

    public bool TryGetResult(
        out Result result,
        out SurrealError error);
}