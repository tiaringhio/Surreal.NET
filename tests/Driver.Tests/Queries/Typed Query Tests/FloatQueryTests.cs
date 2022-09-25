namespace SurrealDB.Driver.Tests.Queries;

public class RpcFloatQueryTests : FloatQueryTests<DatabaseRpc, RpcResponse> { }
public class RestFloatQueryTests : FloatQueryTests<DatabaseRest, RestResponse> { }

public abstract class FloatQueryTests <T, U> : MathQueryTests<T, U, float, float>
    where T : IDatabase<U>, new()
    where U : IResponse {

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
}
