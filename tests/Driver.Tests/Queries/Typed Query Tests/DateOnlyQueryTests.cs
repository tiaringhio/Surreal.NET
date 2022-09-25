using Xunit.Abstractions;

namespace SurrealDB.Driver.Tests.Queries;
public class RpcDateOnlyQueryTests : DateOnlyQueryTests<DatabaseRpc, RpcResponse> {
    public RpcDateOnlyQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RestDateOnlyQueryTests : DateOnlyQueryTests<DatabaseRest, RestResponse> {
    public RestDateOnlyQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class DateOnlyQueryTests<T, U> : InequalityQueryTests<T, U, int, DateOnly>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override DateOnly RandomValue() {
        return RandomDateOnly();
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }

    private static DateOnly RandomDateOnly() {
        var minDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maxDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var diff = (maxDate - minDate).TotalMicroseconds();
        var randomeDateTime = minDate.AddMicroseconds((long)(Random.Shared.NextDouble() * diff));
        return DateOnly.FromDateTime(randomeDateTime);
    }

    protected DateOnlyQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
