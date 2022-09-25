using Xunit.Abstractions;

namespace SurrealDB.Driver.Tests.Queries;

public class RestStringQueryTests : StringQueryTests<DatabaseRest, RestResponse> {
    public RestStringQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RpcStringQueryTests : StringQueryTests<DatabaseRpc, RpcResponse> {
    public RpcStringQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class StringQueryTests<T, U> : EqualityQueryTests<T, U, string, string>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override string RandomKey() {
        return RandomString();
    }

    protected override string RandomValue() {
        return RandomString();
    }

    private static string RandomString(int length = 10) {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }

    public StringQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
