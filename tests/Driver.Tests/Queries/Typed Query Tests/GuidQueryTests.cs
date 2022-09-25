using Xunit.Abstractions;

namespace SurrealDB.Driver.Tests.Queries;

public class RpcGuidQueryTests : GuidQueryTests<DatabaseRpc, RpcResponse> {
    public RpcGuidQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestGuidQueryTests : GuidQueryTests<DatabaseRest, RestResponse> {
    public RestGuidQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class GuidQueryTests<T, U> : EqualityQueryTests<T, U, Guid, Guid>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override Guid RandomKey() {
        return RandomGuid();
    }

    protected override Guid RandomValue() {
        return RandomGuid();
    }

    private static Guid RandomGuid() {
        return Guid.NewGuid();
    }

    public GuidQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
