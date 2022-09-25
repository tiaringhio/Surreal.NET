using Xunit.Abstractions;

namespace SurrealDB.Driver.Tests.Queries;

public abstract class InequalityQueryTests<T, U, TKey, TValue> : EqualityQueryTests<T, U, TKey, TValue>
    where T : IDatabase<U>, new()
    where U : IResponse {

    [Fact]
    public async Task LessThanQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult = (dynamic)val1! < (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

        string sql = $"SELECT * FROM ($val1 < $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var resultValue = result.GetObject<bool>();
        Assert.Equal(resultValue, expectedResult);
    }

    [Fact]
    public async Task LessThanOrEqualToQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult = (dynamic)val1! <= (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

        string sql = $"SELECT * FROM ($val1 <= $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var resultValue = result.GetObject<bool>();
        Assert.Equal(resultValue, expectedResult);
    }

    [Fact]
    public async Task GreaterThanQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult = (dynamic)val1! > (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

        string sql = $"SELECT * FROM ($val1 > $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var resultValue = result.GetObject<bool>();
        Assert.Equal(resultValue, expectedResult);
    }

    [Fact]
    public async Task GreaterThanOrEqualToQueryTest() {
        var val1 = RandomValue();
        var val2 = RandomValue();
        var expectedResult = (dynamic)val1! >= (dynamic)val2!; // Can't do operator overloads on generic types, so force it by casting to a dynamic

        string sql = $"SELECT * FROM ($val1 >= $val2)";
        Dictionary<string, object?> param = new() {
            ["val1"] = val1,
            ["val2"] = val2,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out Result result));
        var resultValue = result.GetObject<bool>();
        Assert.Equal(resultValue, expectedResult);
    }

    public InequalityQueryTests(ITestOutputHelper logger) : base(logger) {
    }
}
