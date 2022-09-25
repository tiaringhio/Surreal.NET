namespace SurrealDB.Driver.Tests.Queries;

public class RpcEnumQueryTests : EnumQueryTests<DatabaseRpc, RpcResponse> { }
public class RestEnumQueryTests : EnumQueryTests<DatabaseRest, RestResponse> { }

public abstract class EnumQueryTests<T, U> : EqualityQueryTests<T, U, int, StandardEnum>
    where T : IDatabase<U>, new()
    where U : IResponse {

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
}
