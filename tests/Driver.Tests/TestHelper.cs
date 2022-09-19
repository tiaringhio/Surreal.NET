using System.Runtime.CompilerServices;

namespace Surreal.Net.Tests;
public static class TestHelper {
    

    public static void AssertOk(
        in ISurrealResponse rpcResponse,
        [CallerArgumentExpression("rpcResponse")]
        string caller = "") {
        if (!rpcResponse.TryGetError(out SurrealError err)) {
            return;
        }

        Exception ex = new($"Expected Ok, got {err.Code} ({err.Message}) in {caller}");
        throw ex;
    }
}
