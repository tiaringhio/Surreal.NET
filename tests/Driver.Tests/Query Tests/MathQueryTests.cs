namespace SurrealDB.Driver.Tests.Queries;

public abstract class MathQueryTests<T, U, TKey, TValue> : InequalityQueryTests<T, U, TKey, TValue>
    where T : IDatabase<U>, new()
    where U : IResponse {
    
    protected abstract string ValueCast();
    protected abstract void AssertEquivalency(TValue a, TValue b);

    [Fact]
    public async Task AdditionQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult = (dynamic)val1! + (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

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
        var expectedResult = (dynamic)val1! - (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

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
        var expectedResult = (dynamic)val1! * (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

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
        var expectedResult = (dynamic)val1! / (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

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
