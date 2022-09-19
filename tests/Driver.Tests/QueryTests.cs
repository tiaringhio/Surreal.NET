using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Surreal.Net.Database;

namespace Surreal.Net.Tests;

public class RestStringQueryTests : StringQueryTests<DbRest, SurrealRestResponse> { }
public class RpcStringQueryTests : StringQueryTests<DbRpc, SurrealRpcResponse> { }

public class RpcIntQueryTests : IntQueryTests<DbRpc, SurrealRpcResponse> { }
public class RestIntQueryTests : IntQueryTests<DbRest, SurrealRestResponse> { }

public class RpcGuidQueryTests : GuidQueryTests<DbRpc, SurrealRpcResponse> { }
public class RestGuidQueryTests : GuidQueryTests<DbRest, SurrealRestResponse> { }

public abstract class StringQueryTests <T, U> : QueryTests<T, U, string, string>
    where T : ISurrealDatabase<U>, new()
    where U : ISurrealResponse {

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

public abstract class IntQueryTests <T, U> : QueryTests<T, U, int, int>
    where T : ISurrealDatabase<U>, new()
    where U : ISurrealResponse {

    protected override int RandomKey() {
        return RandomInt();
    }

    protected override int RandomValue() {
        return RandomInt();
    }

    private static int RandomInt() {
        return Random.Shared.Next();
    }
}

public abstract class GuidQueryTests <T, U> : QueryTests<T, U, Guid, Guid>
    where T : ISurrealDatabase<U>, new()
    where U : ISurrealResponse {

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

public abstract class QueryTests<T, U, TKey, TValue>
    where T : ISurrealDatabase<U>, new()
    where U : ISurrealResponse {
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
        
        SurrealThing thing = SurrealThing.From("object", expectedObject.Key!.ToString());
        await Database.Create(thing, expectedObject);
        string sql = "SELECT * FROM $thing";
        Dictionary<string, object?> param = new() {
            ["thing"] = thing,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out SurrealResult result));
        Assert.True(result.TryGetObjectCollection(out List<TestObject<TKey, TValue>>? returnedDocuments));
        Assert.Single(returnedDocuments);
        var returnedDocument = returnedDocuments.Single();
        Assert.IsType<TestObject<TKey, TValue>>(returnedDocument);
        expectedObject.Should().BeEquivalentTo(returnedDocument);
    }

    [Fact]
    public async Task SimpleSelectFromWhereQueryTest() {
        var expectedObject = new TestObject<TKey, TValue>(RandomKey(), RandomValue());

        SurrealThing thing = SurrealThing.From("object", expectedObject.Key!.ToString());
        await Database.Create(thing, expectedObject);

        string sql = "SELECT * FROM object WHERE id = $thing";
        Dictionary<string, object?> param = new() {
            ["thing"] = thing
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out SurrealResult result));
        Assert.True(result.TryGetObjectCollection(out List<TestObject<TKey, TValue>>? returnedDocuments));
        Assert.Single(returnedDocuments);
        var returnedDocument = returnedDocuments.Single();
        Assert.IsType<TestObject<TKey, TValue>>(returnedDocument);
        expectedObject.Should().BeEquivalentTo(returnedDocument);
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