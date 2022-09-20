using System.Runtime.CompilerServices;

using SurrealDB.Models;

namespace SurrealDB.Driver.Tests;
public static class TestHelper {
    

    public static void AssertOk(
        in IResponse rpcResponse,
        [CallerArgumentExpression("rpcResponse")]
        string caller = "") {
        if (!rpcResponse.TryGetError(out SurrealError err)) {
            return;
        }

        Exception ex = new($"Expected Ok, got error code {err.Code} ({err.Message}) in {caller}");
        throw ex;
    }
}
