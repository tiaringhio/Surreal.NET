using SurrealDB.Json;

namespace SurrealDB.Driver.Tests.Queries;

[Collection("SurrealDBRequired")]
public abstract class QueryTests<T, TKey, TValue>
    where T : IDatabase, IDisposable, new() {

    TestDatabaseFixture? fixture;
    protected readonly ITestOutputHelper Logger;

    public QueryTests(ITestOutputHelper logger) {
        Logger = logger;
    }

    protected abstract TKey RandomKey();
    protected abstract TValue RandomValue();

    [Fact]
    public async Task SimpleSelectQueryTest() => await DbHandle<T>.WithDatabase(
        async db => {
            TestObject<TKey, TValue> expectedObject = new(RandomKey(), RandomValue());

            Thing thing = Thing.From("object", expectedObject.Key!.ToString());
            await db.Create(thing, expectedObject);
            string sql = "SELECT * FROM $thing";
            Dictionary<string, object?> param = new() { ["thing"] = thing, };

            var response = await db.Query(sql, param);

            Assert.NotNull(response);
            TestHelper.AssertOk(response);
            Assert.True(response.TryGetResult(out Result result));
            TestObject<TKey, TValue>? doc = result.GetObject<TestObject<TKey, TValue>>();
            doc.Should().BeEquivalentTo(expectedObject);
        }
    );

    [Fact]
    public async Task SimpleSelectFromWhereQueryTest() => await DbHandle<T>.WithDatabase(
        async db => {
            TestObject<TKey, TValue> expectedObject = new(RandomKey(), RandomValue());

            Thing thing = Thing.From("object", expectedObject.Key!.ToString());
            await db.Create(thing, expectedObject);

            string sql = "SELECT * FROM object WHERE id = $thing";
            Dictionary<string, object?> param = new() { ["thing"] = thing };

            var response = await db.Query(sql, param);

            Assert.NotNull(response);
            TestHelper.AssertOk(response);
            Assert.True(response.TryGetResult(out Result result));
            TestObject<TKey, TValue>? doc = result.GetObject<TestObject<TKey, TValue>>();
            doc.Should().BeEquivalentTo(expectedObject);
        }
    );

    [Fact]
    public async Task SimpleSelectFromWhereValueQueryTest() => await DbHandle<T>.WithDatabase(
        async db => {
            TestObject<TKey, TValue> expectedObject = new(RandomKey(), RandomValue());
            Logger.WriteLine("exp: {0}", Serialize(expectedObject));

            Thing thing = Thing.From("stuff", expectedObject.Key!.ToString());
            await db.Create(thing, expectedObject);

            string sql = "SELECT * FROM stuff WHERE Value = $value";
            Dictionary<string, object?> param = new() { ["value"] = expectedObject.Value };

            var response = await db.Query(sql, param);
            Logger.WriteLine("rsp: {0}", response);

            Assert.NotNull(response);
            TestHelper.AssertOk(response);
            Assert.True(response.TryGetResult(out Result result));
            TestObject<TKey, TValue>? doc = result.GetObject<TestObject<TKey, TValue>>();
            Logger.WriteLine("out: {0}", Serialize(doc));
            doc.Should().BeEquivalentTo(expectedObject);
        }
    );

    [DebuggerStepThrough]
    protected static string Serialize<V>(in V value) {
        return JsonSerializer.Serialize(value, Constants.JsonOptions);
    }

    [DebuggerStepThrough]
    protected static V? Deserialize<V>(string value) {
        return JsonSerializer.Deserialize<V>(value, Constants.JsonOptions);

    }
}
