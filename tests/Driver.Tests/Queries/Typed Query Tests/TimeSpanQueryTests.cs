namespace SurrealDB.Driver.Tests.Queries;
public class RpcTimeSpanQueryTests : TimeSpanQueryTests<DatabaseRpc, RpcResponse> { }
public class RestTimeSpanQueryTests : TimeSpanQueryTests<DatabaseRest, RestResponse> { }

public abstract class TimeSpanQueryTests<T, U> : InequalityQueryTests<T, U, int, TimeSpan>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override TimeSpan RandomValue() {
        return RandomTimeOnly();
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }

    private static TimeSpan RandomTimeOnly() {
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var diff = (maxDate - minDate);
        return diff;
    }
}
