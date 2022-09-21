namespace SurrealDB.Driver.Tests.Queries;

public class RestStringQueryTests : StringQueryTests<DatabaseRest, RestResponse> { }
public class RpcStringQueryTests : StringQueryTests<DatabaseRpc, RpcResponse> { }

public class RpcGuidQueryTests : GuidQueryTests<DatabaseRpc, RpcResponse> { }
public class RestGuidQueryTests : GuidQueryTests<DatabaseRest, RestResponse> { }

public class RpcIntQueryTests : IntQueryTests<DatabaseRpc, RpcResponse> { }
public class RestIntQueryTests : IntQueryTests<DatabaseRest, RestResponse> { }

public class RpcLongQueryTests : LongQueryTests<DatabaseRpc, RpcResponse> { }
public class RestLongQueryTests : LongQueryTests<DatabaseRest, RestResponse> { }

public class RpcFloatQueryTests : FloatQueryTests<DatabaseRpc, RpcResponse> { }
public class RestFloatQueryTests : FloatQueryTests<DatabaseRest, RestResponse> { }

public class RpcDoubleQueryTests : DoubleQueryTests<DatabaseRpc, RpcResponse> { }
public class RestDoubleQueryTests : DoubleQueryTests<DatabaseRest, RestResponse> { }

public abstract class StringQueryTests <T, U> : QueryTests<T, U, string, string>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected StringQueryTests() {
        TestHelper.EnsureDB();
    }

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
}

public abstract class IntQueryTests <T, U> : MathQueryTests<T, U, int, int>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override int RandomValue() {
        return Random.Shared.Next(-10000, 10000); // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }

    protected override void AssertEquivalency(int a, int b) {
        b.Should().Be(a);
    }
}

public abstract class LongQueryTests <T, U> : MathQueryTests<T, U, long, long>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override long RandomKey() {
        return RandomLong();
    }

    protected override long RandomValue() {
        return Random.Shared.NextInt64(-10000, 10000); // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static long RandomLong() {
        return Random.Shared.NextInt64();
    }

    protected override void AssertEquivalency(long a, long b) {
        b.Should().Be(a);
    }
}

public abstract class FloatQueryTests <T, U> : MathQueryTests<T, U, float, float>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override float RandomKey() {
        return RandomFloat();
    }

    protected override float RandomValue() {
        return (RandomFloat() * 2000) - 1000; // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<float>";
    }

    private static float RandomFloat() {
        return Random.Shared.NextSingle();
    }

    protected override void AssertEquivalency(float a, float b) {
        b.Should().BeApproximately(a, 0.1f);
    }
}

public abstract class DoubleQueryTests <T, U> : MathQueryTests<T, U, double, double>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override double RandomKey() {
        return RandomDouble();
    }

    protected override double RandomValue() {
        return (RandomDouble() * 2000d) - 1000d; // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<float>";
    }

    private static double RandomDouble() {
        return Random.Shared.NextDouble();
    }

    protected override void AssertEquivalency(double a, double b) {
        b.Should().BeApproximately(a, 0.1f);
    }
}

public abstract class GuidQueryTests<T, U> : QueryTests<T, U, Guid, Guid>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override Guid RandomKey() {
        return RandomGuid();
    }

    protected override Guid RandomValue() {
        return RandomGuid();
    }

    private static Guid RandomGuid() {
        return Guid.NewGuid();
    }
}

public abstract class MathQueryTests<T, U, TKey, TValue> : QueryTests<T, U, TKey, TValue>
    where T : IDatabase<U>, new()
    where U : IResponse {
    
    protected abstract string ValueCast();
    protected abstract void AssertEquivalency(TValue a, TValue b);

    [Fact]
    public async Task AdditionQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult =  (dynamic)val1! + (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

        string sql = $"SELECT * FROM {ValueCast()}($val1 + $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var resultValue = result.GetObject<TValue>();
        AssertEquivalency(resultValue, expectedResult);
    }

    [Fact]
    public async Task SubtractionQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult =  (dynamic)val1! - (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

        string sql = $"SELECT * FROM {ValueCast()}($val1 - $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var value = result.GetObject<TValue>();
        AssertEquivalency(value, expectedResult);
    }

    [Fact]
    public async Task MultiplicationQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult =  (dynamic)val1! * (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

        string sql = $"SELECT * FROM {ValueCast()}($val1 * $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var value = result.GetObject<TValue>();
        AssertEquivalency(value, expectedResult);
    }

    [Fact]
    public async Task DivisionQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult =  (dynamic)val1! / (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

        string sql = $"SELECT * FROM {ValueCast()}($val1 / $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var value = result.GetObject<TValue>();
        AssertEquivalency(value, expectedResult);
    }
}

public abstract class QueryTests<T, U, TKey, TValue>
    where T : IDatabase<U>, new()
    where U : IResponse {
    protected T Database;

    protected QueryTests() {
        Database = new();
        Database.Open(TestHelper.Default).Wait();
    }

    protected abstract TKey RandomKey();
    protected abstract TValue RandomValue();

    [Fact]
    public async Task SimpleSelectQueryTest() {
        var expectedObject = new TestObject<TKey, TValue>(RandomKey(), RandomValue());
        
        Thing thing = Thing.From("object", expectedObject.Key!.ToString());
        await Database.Create(thing, expectedObject);
        string sql = "SELECT * FROM $thing";
        Dictionary<string, object?> param = new() {
            ["thing"] = thing,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var doc = result.GetObject<TestObject<TKey, TValue>>();
        doc.Should().BeEquivalentTo(expectedObject);
    }

    [Fact]
    public async Task SimpleSelectFromWhereQueryTest() {
        var expectedObject = new TestObject<TKey, TValue>(RandomKey(), RandomValue());

        Thing thing = Thing.From("object", expectedObject.Key!.ToString());
        await Database.Create(thing, expectedObject);

        string sql = "SELECT * FROM object WHERE id = $thing";
        Dictionary<string, object?> param = new() {
            ["thing"] = thing
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var doc = result.GetObject<TestObject<TKey, TValue>>();
        doc.Should().BeEquivalentTo(expectedObject);
    }

    [Fact]
    public async Task SimpleSelectFromWhereValueQueryTest() {
        var expectedObject = new TestObject<TKey, TValue>(RandomKey(), RandomValue());

        Thing thing = Thing.From("object", expectedObject.Key!.ToString());
        await Database.Create(thing, expectedObject);

        string sql = "SELECT * FROM object WHERE Value = $value";
        Dictionary<string, object?> param = new() {
            ["value"] = expectedObject.Value
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var doc = result.GetObject<TestObject<TKey, TValue>>();
        doc.Should().BeEquivalentTo(expectedObject);
    }
}

public class TestObject<TKey, TValue> {
    [JsonConstructor]
    public TestObject(TKey key, TValue value) {
        Key = key;
        Value = value;
    }

    public TKey Key { get; set; }
    public TValue Value { get; set; }
}