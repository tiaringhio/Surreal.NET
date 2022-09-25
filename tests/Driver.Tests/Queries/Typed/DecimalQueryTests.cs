namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RpcDecimalQueryTests : DecimalQueryTests<DatabaseRpc> {
    public RpcDecimalQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestDecimalQueryTests : DecimalQueryTests<DatabaseRest> {
    public RestDecimalQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class DecimalQueryTests <T> : MathQueryTests<T, decimal, decimal>
    where T : IDatabase, IDisposable, new() {

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
        return (decimal)ThreadRng.Shared.NextDouble();
    }

    protected override void AssertEquivalency(decimal a, decimal b) {
        b.Should().BeApproximately(a, 0.1m);
    }

    protected DecimalQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
