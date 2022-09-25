namespace SurrealDB.Driver.Tests.Queries.Typed;
public class RpcTimeOnlyQueryTests : TimeOnlyQueryTests<DatabaseRpc> {
    public RpcTimeOnlyQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestTimeOnlyQueryTests : TimeOnlyQueryTests<DatabaseRest> {
    public RestTimeOnlyQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class TimeOnlyQueryTests<T> : InequalityQueryTests<T, int, TimeOnly>
    where T : IDatabase, IDisposable, new(){

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override TimeOnly RandomValue() {
        return RandomTimeOnly();
    }

    private static int RandomInt() {
        return ThreadRng.Shared.Next();
    }

    private static TimeOnly RandomTimeOnly() {
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var diff = (maxDate - minDate).TotalMicroseconds();
        var randomeDateTime = minDate.AddMicroseconds((long)(ThreadRng.Shared.NextDouble() * diff));
        return TimeOnly.FromDateTime(randomeDateTime);
    }

    public TimeOnlyQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
