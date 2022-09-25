namespace SurrealDB.Driver.Tests.Queries;

public class RpcIntQueryTests : IntQueryTests<DatabaseRpc, RpcResponse> { }
public class RestIntQueryTests : IntQueryTests<DatabaseRest, RestResponse> { }

public abstract class IntQueryTests <T, U> : MathQueryTests<T, U, int, int>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override int RandomValue() {
        return Random.Shared.Next(-10000, 10000); // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }

    protected override void AssertEquivalency(int a, int b) {
        b.Should().Be(a);
    }
}
