using System.Text.Json;

using SurrealDB.Models.DriverResult;

namespace SurrealDB.Models;

public interface IResponse : IEnumerable<RawResult> {
    IEnumerable<OkResult> Oks { get; }
    IEnumerable<ErrorResult> Errors { get; }

    bool HasErrors { get; }
    bool IsEmpty { get; }
}
