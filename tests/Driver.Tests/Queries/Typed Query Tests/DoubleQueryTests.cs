using Xunit.Abstractions;

namespace SurrealDB.Driver.Tests.Queries;

public class RpcDoubleQueryTests : DoubleQueryTests<DatabaseRpc, RpcResponse> {
    public RpcDoubleQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestDoubleQueryTests : DoubleQueryTests<DatabaseRest, RestResponse> {
    public RestDoubleQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class DoubleQueryTests <T, U> : MathQueryTests<T, U, double, double>
    where T : IDatabase<U>, new()
    where U : IResponse {

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
