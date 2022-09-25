namespace SurrealDB.Driver.Tests.Queries;

public class RpcLongQueryTests : LongQueryTests<DatabaseRpc, RpcResponse> { }
public class RestLongQueryTests : LongQueryTests<DatabaseRest, RestResponse> { }

public abstract class LongQueryTests <T, U> : MathQueryTests<T, U, long, long>
    where T : IDatabase<U>, new()
    where U : IResponse {

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
}
