namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RpcLongQueryTests : LongQueryTests<DatabaseRpc> {
    public RpcLongQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestLongQueryTests : LongQueryTests<DatabaseRest> {
    public RestLongQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class LongQueryTests <T> : MathQueryTests<T, long, long>
    where T : IDatabase, IDisposable, new() {

    protected override long RandomKey() {
        return RandomLong();
    }

    protected override long RandomValue() {
        return Random.Shared.NextInt64(-10000, 10000); // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static long RandomLong() {
        return Random.Shared.NextInt64();
    }

    protected override void AssertEquivalency(long a, long b) {
        b.Should().Be(a);
    }

    protected LongQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
