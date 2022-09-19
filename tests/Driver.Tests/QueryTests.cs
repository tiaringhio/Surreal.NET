using System.Text.Json.Serialization;

using SurrealDB.Abstractions;
using SurrealDB.Driver.Rest;
using SurrealDB.Driver.Rpc;
using SurrealDB.Models;

namespace SurrealDB.Driver.Tests;

public class RestStringQueryTests : StringQueryTests<DatabaseRest, RestResponse> { }
public class RpcStringQueryTests : StringQueryTests<DatabaseRpc, RpcResponse> { }

public class RpcIntQueryTests : IntQueryTests<DatabaseRpc, RpcResponse> { }
public class RestIntQueryTests : IntQueryTests<DatabaseRest, RestResponse> { }

public class RpcGuidQueryTests : GuidQueryTests<DatabaseRpc, RpcResponse> { }
public class RestGuidQueryTests : GuidQueryTests<DatabaseRest, RestResponse> { }

public class RpcFloatQueryTests : FloatQueryTests<DatabaseRpc, RpcResponse> { }
public class RestFloatQueryTests : FloatQueryTests<DatabaseRest, RestResponse> { }

public abstract class StringQueryTests <T, U> : QueryTests<T, U, string, string>
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
}

public abstract class IntQueryTests <T, U> : MathQueryTests<T, U, int, int>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override int RandomValue() {
        return 7; // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }
}

public abstract class FloatQueryTests <T, U> : MathQueryTests<T, U, float, float>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override float RandomKey() {
        return RandomFloat();
    }

    protected override float RandomValue() {
        return 7f; // Can't go too high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<float>";
    }

    private static float RandomFloat() {
        return Random.Shared.NextSingle();
    }
}

public abstract class DoubleQueryTests <T, U> : MathQueryTests<T, U, int, int>
    where T : IDatabase<U>, new()
    where U : IResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override int RandomValue() {
        return 7; // Can't go to high otherwise the maths operations might overflow
    }

    protected override string ValueCast() {
        return "<int>";
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }
}

public abstract class GuidQueryTests <T, U> : QueryTests<T, U, Guid, Guid>
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

    [Fact]
    public async Task AdditionQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult =  (dynamic)val1! + (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a generic

        string sql = $"SELECT * FROM {ValueCast()}($val1 + $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        Assert.True(result.TryGetObject(out TValue? doc));
        Assert.IsType<TValue>(doc);
        doc.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task SubtractionQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult =  (dynamic)val1! - (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a generic

        string sql = $"SELECT * FROM {ValueCast()}($val1 - $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        Assert.True(result.TryGetObject(out TValue? doc));
        Assert.IsType<TValue>(doc);
        doc.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task MultiplicationQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult =  (dynamic)val1! * (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a generic

        string sql = $"SELECT * FROM {ValueCast()}($val1 * $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        Assert.True(result.TryGetObject(out TValue? doc));
        Assert.IsType<TValue>(doc);
        doc.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task DivisionQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult =  (dynamic)val1! / (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a generic

        string sql = $"SELECT * FROM {ValueCast()}($val1 / $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        Assert.True(result.TryGetObject(out TValue? doc));
        Assert.IsType<TValue>(doc);
        doc.Should().BeEquivalentTo(expectedResult);
    }
}

public abstract class QueryTests<T, U, TKey, TValue>
    where T : IDatabase<U>, new()
    where U : IResponse {
    protected T Database;

    protected QueryTests() {
        Database = new();
        Database.Open(ConfigHelper.Default).Wait();
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
        Assert.True(result.TryGetObject(out TestObject<TKey, TValue>? doc));
        Assert.IsType<TestObject<TKey, TValue>>(doc);
        expectedObject.Should().BeEquivalentTo(doc);
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
        Assert.True(result.TryGetObject(out TestObject<TKey, TValue>? doc));
        Assert.IsType<TestObject<TKey, TValue>>(doc);
        expectedObject.Should().BeEquivalentTo(doc);
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
        Assert.True(result.TryGetObject(out TestObject<TKey, TValue>? doc));
        Assert.IsType<TestObject<TKey, TValue>>(doc);
        expectedObject.Should().BeEquivalentTo(doc);
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