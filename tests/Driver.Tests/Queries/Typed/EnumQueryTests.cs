namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RpcEnumQueryTests : EnumQueryTests<DatabaseRpc> {
    public RpcEnumQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestEnumQueryTests : EnumQueryTests<DatabaseRest> {
    public RestEnumQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class EnumQueryTests<T> : EqualityQueryTests<T, int, StandardEnum>
    where T : IDatabase, IDisposable, new() {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override StandardEnum RandomValue() {
        return RandomEnum();
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }

    private static StandardEnum RandomEnum() {
        var enumValues = Enum.GetValues<StandardEnum>();
        var index = Random.Shared.Next(0, enumValues.Length);
        return enumValues[index];
    }

    public EnumQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
