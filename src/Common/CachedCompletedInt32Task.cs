using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SurrealDB.Common;

/// <summary>
///     Encapsulates the logic of caching the last synchronously completed task of integer.
///     Used in classes like <see cref="MemoryStream" /> to reduce allocations.
/// </summary>
/// <remarks>
///     Source: https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Threading/Tasks/CachedCompletedInt32Task.cs
/// </remarks>
#if SURREAL_NET_INTERNAL
public
#else
internal
#endif
    struct CachedCompletedInt32Task {
    private Task<int>? _task;

    /// <summary> Gets a completed <see cref="Task{Int32}" /> whose result is <paramref name="result" />. </summary>
    /// <remarks> This method will try to return an already cached task if available. </remarks>
    /// <param name="result"> The result value for which a <see cref="Task{Int32}" /> is needed. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<int> GetTask(int result) {
        if (_task is { } task) {
            Debug.Assert(task.IsCompletedSuccessfully, "Expected that a stored last task completed successfully");
            if (task.Result == result) {
                return task;
            }
        }

        return _task = Task.FromResult(result);
    }
}