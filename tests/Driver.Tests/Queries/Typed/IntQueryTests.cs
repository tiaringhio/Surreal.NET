namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RpcIntQueryTests : IntQueryTests<DatabaseRpc> {
    public RpcIntQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestIntQueryTests : IntQueryTests<DatabaseRest> {
    public RestIntQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class IntQueryTests <T> : MathQueryTests<T, int, int>
    where T : IDatabase, IDisposable, new() {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override int RandomValue() {
        return ThreadRng.Shared.Next(-10000, 10000); // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static int RandomInt() {
        return ThreadRng.Shared.Next();
    }

    protected override void AssertEquivalency(int a, int b) {
        b.Should().Be(a);
    }

    protected IntQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
