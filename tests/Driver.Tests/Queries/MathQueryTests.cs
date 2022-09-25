namespace SurrealDB.Driver.Tests.Queries;

public abstract class MathQueryTests<T, TKey, TValue> : InequalityQueryTests<T, TKey, TValue>
    where T : IDatabase, IDisposable, new() {

    protected abstract string ValueCast();
    protected abstract void AssertEquivalency(TValue a, TValue b);

    [Fact]
    public async Task AdditionQueryTest() => await DbHandle<T>.WithDatabase(
        async db => {
            var val1 = RandomValue();
            var val2 = RandomValue();
            var expectedResult = (dynamic)val1! + (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

            string sql = $"SELECT * FROM {ValueCast()}($val1 + $val2)";
            Dictionary<string, object?> param = new() { ["val1"] = val1, ["val2"] = val2, };

            var response = await db.Query(sql, param);

            Assert.NotNull(response);
            TestHelper.AssertOk(response);
            Assert.True(response.TryGetResult(out Result result));
            var resultValue = result.GetObject<TValue>();
            AssertEquivalency(resultValue, expectedResult);
        }
    );

    [Fact]
    public async Task SubtractionQueryTest() => await DbHandle<T>.WithDatabase(
        async db => {
            var val1 = RandomValue();
            var val2 = RandomValue();
            var expectedResult = (dynamic)val1! - (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

            string sql = $"SELECT * FROM {ValueCast()}($val1 - $val2)";
            Dictionary<string, object?> param = new() { ["val1"] = val1, ["val2"] = val2, };

            var response = await db.Query(sql, param);

            Assert.NotNull(response);
            TestHelper.AssertOk(response);
            Assert.True(response.TryGetResult(out Result result));
            var value = result.GetObject<TValue>();
            AssertEquivalency(value, expectedResult);
        }
    );

    [Fact]
    public async Task MultiplicationQueryTest() => await DbHandle<T>.WithDatabase(
        async db => {
            var val1 = RandomValue();
            var val2 = RandomValue();
            var expectedResult = (dynamic)val1! * (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

            string sql = $"SELECT * FROM {ValueCast()}($val1 * $val2)";
            Dictionary<string, object?> param = new() { ["val1"] = val1, ["val2"] = val2, };

            var response = await db.Query(sql, param);

            Assert.NotNull(response);
            TestHelper.AssertOk(response);
            Assert.True(response.TryGetResult(out Result result));
            var value = result.GetObject<TValue>();
            AssertEquivalency(value, expectedResult);
        }
    );

    [Fact]
    public async Task DivisionQueryTest() => await DbHandle<T>.WithDatabase(
        async db => {
            var val1 = RandomValue();
            var val2 = RandomValue();
            var expectedResult = (dynamic)val1! / (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

            string sql = $"SELECT * FROM {ValueCast()}($val1 / $val2)";
            Dictionary<string, object?> param = new() { ["val1"] = val1, ["val2"] = val2, };

            var response = await db.Query(sql, param);

            Assert.NotNull(response);
            TestHelper.AssertOk(response);
            Assert.True(response.TryGetResult(out Result result));
            var value = result.GetObject<TValue>();
            AssertEquivalency(value, expectedResult);
        }
    );

    protected MathQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
