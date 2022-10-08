using System.Text.Json;

namespace SurrealDB.Models;

public interface IResponse : IEnumerable<RawResult> {
    IEnumerable<OkResult> Oks { get; }
    IEnumerable<ErrorResult> Errors { get; }

    bool HasErrors { get; }
    bool IsEmpty { get; }

    public bool TryGetFirstError(out ErrorResult err);

    public ErrorResult FirstError { get; }

    public bool TryGetFirstOk(out OkResult ok);

    public OkResult FirstOk { get; }

    public bool TryGetSingleError(out ErrorResult err);

    public ErrorResult SingleError { get; }

    public bool TryGetSingleOk(out OkResult ok);

    public OkResult SingleOk { get; }

}
