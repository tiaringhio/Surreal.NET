namespace SurrealDB.Driver.Tests.Queries.Typed;
public class RpcTimeSpanQueryTests : TimeSpanQueryTests<DatabaseRpc> {
    public RpcTimeSpanQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestTimeSpanQueryTests : TimeSpanQueryTests<DatabaseRest> {
    public RestTimeSpanQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class TimeSpanQueryTests<T> : InequalityQueryTests<T, int, TimeSpan>
    where T : IDatabase, IDisposable, new() {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override TimeSpan RandomValue() {
        return RandomTimeOnly();
    }

    private static int RandomInt() {
        return ThreadRng.Shared.Next();
    }

    private static TimeSpan RandomTimeOnly() {
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var diff = (maxDate - minDate);
        return diff;
    }

    public TimeSpanQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
