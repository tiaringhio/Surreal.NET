namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RpcGuidQueryTests : GuidQueryTests<DatabaseRpc> {
    public RpcGuidQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestGuidQueryTests : GuidQueryTests<DatabaseRest> {
    public RestGuidQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class GuidQueryTests<T> : EqualityQueryTests<T, Guid, Guid>
    where T : IDatabase, IDisposable, new() {

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
