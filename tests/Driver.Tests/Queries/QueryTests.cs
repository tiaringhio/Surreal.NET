using SurrealDB.Json;

using Xunit.Abstractions;

namespace SurrealDB.Driver.Tests.Queries;

[Collection("SurrealDBRequired")]
public abstract class QueryTests<T, U, TKey, TValue>
    where T : IDatabase<U>, new()
    where U : IResponse {

    TestDatabaseFixture? fixture;
    protected T Database;
    protected readonly ITestOutputHelper Logger;

    public QueryTests(ITestOutputHelper logger) {
        Logger = logger;
        Database = new();
        Database.Open(TestHelper.Default).Wait();
    }

    protected abstract TKey RandomKey();
    protected abstract TValue RandomValue();

    [Fact]
    public async Task SimpleSelectQueryTest() {
        TestObject<TKey, TValue> expectedObject = new(RandomKey(), RandomValue());

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
        TestObject<TKey, TValue>? doc = result.GetObject<TestObject<TKey, TValue>>();
        doc.Should().BeEquivalentTo(expectedObject);
    }

    [Fact]
    public async Task SimpleSelectFromWhereQueryTest() {
        TestObject<TKey, TValue> expectedObject = new(RandomKey(), RandomValue());

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
        TestObject<TKey, TValue>? doc = result.GetObject<TestObject<TKey, TValue>>();
        doc.Should().BeEquivalentTo(expectedObject);
    }

    [Fact]
    public async Task SimpleSelectFromWhereValueQueryTest() {
        TestObject<TKey, TValue> expectedObject = new(RandomKey(), RandomValue());
        Logger.WriteLine("exp: {0}", Serialize(expectedObject));

        Thing thing = Thing.From("object", expectedObject.Key!.ToString());
        await Database.Create(thing, expectedObject);

        string sql = "SELECT * FROM object WHERE Value = $value";
        Dictionary<string, object?> param = new() {
            ["value"] = expectedObject.Value
        };

        U response = await Database.Query(sql, param);
        Logger.WriteLine("rsp: {0}", response);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        TestObject<TKey, TValue>? doc = result.GetObject<TestObject<TKey, TValue>>();
        Logger.WriteLine("out: {0}", Serialize(doc));
        doc.Should().BeEquivalentTo(expectedObject);
    }

    [DebuggerStepThrough]
    protected static string Serialize<V>(in V value) {
        return JsonSerializer.Serialize(value, Constants.JsonOptions);
    }

    [DebuggerStepThrough]
    protected static V? Deserialize<V>(string value) {
        return JsonSerializer.Deserialize<V>(value, Constants.JsonOptions);

    }
}
