namespace SurrealDB.Driver.Tests.Queries.Typed;
public class RpcDateTimeQueryTests : DateTimeQueryTests<DatabaseRpc> {
    public RpcDateTimeQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestDateTimeQueryTests : DateTimeQueryTests<DatabaseRest> {
    public RestDateTimeQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class DateTimeQueryTests<T> : InequalityQueryTests<T, int, DateTime>
    where T : IDatabase, IDisposable, new() {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override DateTime RandomValue() {
        return RandomDateTime();
    }

    private static int RandomInt() {
        return ThreadRng.Shared.Next();
    }

    private static DateTime RandomDateTime() {
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var diff = (maxDate - minDate).TotalMicroseconds();
        var randomeDateTime = minDate.AddMicroseconds((long)(ThreadRng.Shared.NextDouble() * diff));
        return randomeDateTime;
    }

    protected DateTimeQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
