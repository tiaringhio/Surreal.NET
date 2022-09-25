using System.Diagnostics.CodeAnalysis;

namespace SurrealDB.Models;

public interface IResponse {
    public bool IsOk { get; }

    public bool IsError { get; }

    public bool TryGetError([NotNullWhen(true)] out Error error);

    public bool TryGetResult([NotNullWhen(true)] out Result result);

    public bool TryGetResult(
        out Result result,
        out Error error);
}
