using Xunit.Abstractions;

namespace SurrealDB.Driver.Tests.Queries;

public class RpcEnumFlagQueryTests : EnumFlagQueryTests<DatabaseRpc, RpcResponse> {
    public RpcEnumFlagQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestEnumFlagQueryTests : EnumFlagQueryTests<DatabaseRest, RestResponse> {
    public RestEnumFlagQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class EnumFlagQueryTests<T, U> : EqualityQueryTests<T, U, int, FlagsEnum>
    where T : IDatabase<U>, new()
    where U : IResponse {

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
