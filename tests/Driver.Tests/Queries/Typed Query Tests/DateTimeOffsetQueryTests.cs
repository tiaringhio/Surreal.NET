using Xunit.Abstractions;

namespace SurrealDB.Driver.Tests.Queries;
public class RpcDateTimeOffsetQueryTests : DateTimeOffsetQueryTests<DatabaseRpc, RpcResponse> {
    public RpcDateTimeOffsetQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestDateTimeOffsetQueryTests : DateTimeOffsetQueryTests<DatabaseRest, RestResponse> {
    public RestDateTimeOffsetQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class DateTimeOffsetQueryTests<T, U> : InequalityQueryTests<T, U, int, DateTimeOffset>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override DateTimeOffset RandomValue() {
        return RandomDateTimeOffset();
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }

    private static DateTimeOffset RandomDateTimeOffset() {
        var minDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var maxDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var diff = (maxDate - minDate).TotalMicroseconds();
        var randomeDateTime = minDate.AddMicroseconds((long)(Random.Shared.NextDouble() * diff));
        return randomeDateTime;
    }

    public DateTimeOffsetQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
