namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RpcFloatQueryTests : FloatQueryTests<DatabaseRpc> {
    public RpcFloatQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestFloatQueryTests : FloatQueryTests<DatabaseRest> {
    public RestFloatQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class FloatQueryTests <T> : MathQueryTests<T, float, float>
    where T : IDatabase, IDisposable, new() {

    protected override float RandomKey() {
        return RandomFloat();
    }

    protected override float RandomValue() {
        return (RandomFloat() * 2000) - 1000; // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<float>";
    }

    private static float RandomFloat() {
        return Random.Shared.NextSingle();
    }

    protected override void AssertEquivalency(float a, float b) {
        b.Should().BeApproximately(a, 0.1f);
    }

    protected FloatQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
