namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RpcEnumFlagQueryTests : EnumFlagQueryTests<DatabaseRpc> {
    public RpcEnumFlagQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestEnumFlagQueryTests : EnumFlagQueryTests<DatabaseRest> {
    public RestEnumFlagQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class EnumFlagQueryTests<T> : EqualityQueryTests<T, int, FlagsEnum>
    where T : IDatabase, IDisposable, new() {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override FlagsEnum RandomValue() {
        return RandomEnum() | RandomEnum();
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }

    private static FlagsEnum RandomEnum() {
        var enumValues = Enum.GetValues<FlagsEnum>();
        var index = Random.Shared.Next(0, enumValues.Length);
        return enumValues[index];
    }

    public EnumFlagQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
