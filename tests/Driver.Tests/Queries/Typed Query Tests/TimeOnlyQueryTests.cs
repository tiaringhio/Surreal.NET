using Xunit.Abstractions;

namespace SurrealDB.Driver.Tests.Queries;
public class RpcTimeOnlyQueryTests : TimeOnlyQueryTests<DatabaseRpc, RpcResponse> {
    public RpcTimeOnlyQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestTimeOnlyQueryTests : TimeOnlyQueryTests<DatabaseRest, RestResponse> {
    public RestTimeOnlyQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class TimeOnlyQueryTests<T, U> : InequalityQueryTests<T, U, int, TimeOnly>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override TimeOnly RandomValue() {
        return RandomTimeOnly();
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }

    private static TimeOnly RandomTimeOnly() {
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2000, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var diff = (maxDate - minDate).TotalMicroseconds();
        var randomeDateTime = minDate.AddMicroseconds((long)(Random.Shared.NextDouble() * diff));
        return TimeOnly.FromDateTime(randomeDateTime);
    }

    public TimeOnlyQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
