namespace SurrealDB.Driver.Tests.Queries.Typed;

public class RestStringQueryTests : StringQueryTests<DatabaseRest> {
    public RestStringQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
public class RpcStringQueryTests : StringQueryTests<DatabaseRpc> {
    public RpcStringQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}

public abstract class StringQueryTests<T> : EqualityQueryTests<T, string, string>
    where T : IDatabase, IDisposable, new() {

    protected override string RandomKey() {
        return RandomString();
    }

    protected override string RandomValue() {
        return RandomString();
    }

    private static string RandomString(int length = 10) {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
           .Select(s => s[ThreadRng.Shared.Next(s.Length)]).ToArray());
    }

    public StringQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
