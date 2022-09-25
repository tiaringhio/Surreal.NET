namespace SurrealDB.Driver.Tests.Queries;

public class RpcDecimalQueryTests : DecimalQueryTests<DatabaseRpc, RpcResponse> { }
public class RestDecimalQueryTests : DecimalQueryTests<DatabaseRest, RestResponse> { }

public abstract class DecimalQueryTests <T, U> : MathQueryTests<T, U, decimal, decimal>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override decimal RandomKey() {
        return RandomDouble();
    }

    protected override decimal RandomValue() {
        return (RandomDouble() * 2000m) - 1000m; // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<float>";
    }

    private static decimal RandomDouble() {
        return (decimal)Random.Shared.NextDouble();
    }

    protected override void AssertEquivalency(decimal a, decimal b) {
        b.Should().BeApproximately(a, 0.1m);
    }
}
