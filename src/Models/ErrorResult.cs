using System.Diagnostics;

using SurrealDB.Common;

namespace SurrealDB.Models;

/// <summary>
///     The result of a failed query to the Surreal database.
/// </summary>
[DebuggerDisplay("{Code,nq}: {Message,nq}")]
public readonly record struct ErrorResult(int Code, string Status, string? Message) {
    public bool IsDefault => MemoryHelper.Compare(in this, default) == 0;
}
