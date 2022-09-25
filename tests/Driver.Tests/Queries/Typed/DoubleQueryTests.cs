namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RpcDoubleQueryTests : DoubleQueryTests<DatabaseRpc> {
    public RpcDoubleQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestDoubleQueryTests : DoubleQueryTests<DatabaseRest> {
    public RestDoubleQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class DoubleQueryTests <T> : MathQueryTests<T, double, double>
    where T : IDatabase, IDisposable, new() {

    protected override double RandomKey() {
        return RandomDouble();
    }

    protected override double RandomValue() {
        return (RandomDouble() * 2000d) - 1000d; // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<float>";
    }

    private static double RandomDouble() {
        return Random.Shared.NextDouble();
    }

    protected override void AssertEquivalency(double a, double b) {
        b.Should().BeApproximately(a, 0.1d);
    }

    protected DoubleQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
