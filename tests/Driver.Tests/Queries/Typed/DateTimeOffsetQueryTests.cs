namespace SurrealDB.Driver.Tests.Queries.Typed;
public class RpcDateTimeOffsetQueryTests : DateTimeOffsetQueryTests<DatabaseRpc> {
    public RpcDateTimeOffsetQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestDateTimeOffsetQueryTests : DateTimeOffsetQueryTests<DatabaseRest> {
    public RestDateTimeOffsetQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class DateTimeOffsetQueryTests<T> : InequalityQueryTests<T, int, DateTimeOffset>
    where T : IDatabase, IDisposable, new() {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override DateTimeOffset RandomValue() {
        return RandomDateTimeOffset();
    }

    private static int RandomInt() {
        return ThreadRng.Shared.Next();
    }

    private static DateTimeOffset RandomDateTimeOffset() {
        var minDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var maxDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var diff = (maxDate - minDate).TotalMicroseconds();
        var randomeDateTime = minDate.AddMicroseconds((long)(ThreadRng.Shared.NextDouble() * diff));
        return randomeDateTime;
    }

    public DateTimeOffsetQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
