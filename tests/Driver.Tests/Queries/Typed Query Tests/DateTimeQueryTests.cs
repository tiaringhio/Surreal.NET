namespace SurrealDB.Driver.Tests.Queries;
public class RpcDateTimeQueryTests : DateTimeQueryTests<DatabaseRpc, RpcResponse> { }
public class RestDateTimeQueryTests : DateTimeQueryTests<DatabaseRest, RestResponse> { }

public abstract class DateTimeQueryTests<T, U> : InequalityQueryTests<T, U, int, DateTime>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override DateTime RandomValue() {
        return RandomDateTime();
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }

    private static DateTime RandomDateTime() {
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var diff = (maxDate - minDate).TotalMicroseconds();
        var randomeDateTime = minDate.AddMicroseconds((long)(Random.Shared.NextDouble() * diff));
        return randomeDateTime;
    }
}
