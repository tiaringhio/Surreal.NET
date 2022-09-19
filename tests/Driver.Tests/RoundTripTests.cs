using System.Runtime.CompilerServices;

using FluentAssertions.Extensions;

using Surreal.Net.Database;

namespace Surreal.Net.Tests;

public class RpcRoundTripTests : RoundTripTests<DbRpc, SurrealRpcResponse> {
}

public class RestRoundTripTests : RoundTripTests<DbRest, SurrealRestResponse> {
}

public abstract class RoundTripTests<T, U>
    where T : ISurrealDatabase<U>, new()
    where U : ISurrealResponse {
    protected T Database;

    protected RoundTripTests() {
        Database = new();
        Database.Open(ConfigHelper.Default).Wait();
    }

    [Fact]
    public async Task CreateRoundTripTest() {
        RoundTripObject expectedObject = new();

        SurrealThing thing = SurrealThing.From("object", Random.Shared.Next().ToString());
        U response = await Database.Create(thing, expectedObject);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out SurrealResult result));
        Assert.True(result.TryGetObject(out RoundTripObject? returnedDocument));
        RoundTripObject.AssertAreEqual(expectedObject, returnedDocument);
    }

    [Fact]
    public async Task CreateAndSelectRoundTripTest() {
        RoundTripObject expectedObject = new();

        SurrealThing thing = SurrealThing.From("object", Random.Shared.Next().ToString());
        await Database.Create(thing, expectedObject);
        U response = await Database.Select(thing);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out SurrealResult result));
        Assert.True(result.TryGetObject(out RoundTripObject? returnedDocument));
        RoundTripObject.AssertAreEqual(expectedObject, returnedDocument);
    }

    [Fact]
    public async Task CreateAndQueryRoundTripTest() {
        RoundTripObject expectedObject = new();

        SurrealThing thing = SurrealThing.From("object", Random.Shared.Next().ToString());
        await Database.Create(thing, expectedObject);
        string sql = $"SELECT * FROM \"{thing}\"";
        U response = await Database.Query(sql, null);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out SurrealResult result));
        Assert.True(result.TryGetObjectCollection(out List<RoundTripObject>? returnedDocument));
        Assert.Single(returnedDocument);
        RoundTripObject.AssertAreEqual(expectedObject, returnedDocument.Single());
    }

    [Fact]
    public async Task CreateAndParameterizedQueryRoundTripTest() {
        RoundTripObject expectedObject = new();

        SurrealThing thing = SurrealThing.From("object", Random.Shared.Next().ToString());
        await Database.Create(thing, expectedObject);
        string sql = "SELECT * FROM $thing";
        Dictionary<string, object?> param = new() {
            ["thing"] = thing,
        };

        U response = await Database.Query(sql, param);

        Assert.NotNull(response);
        TestHelper.AssertOk(response);
        Assert.True(response.TryGetResult(out SurrealResult result));
        Assert.True(result.TryGetObjectCollection(out List<RoundTripObject>? returnedDocument));
        Assert.Single(returnedDocument);
        RoundTripObject.AssertAreEqual(expectedObject, returnedDocument.Single());
    }
}

public class RoundTripObject {
    [Flags]
    public enum FlagsEnum {
        None = 0,
        First = 1 << 0,
        Second = 1 << 1,
        Third = 1 << 2,
        Fourth = 1 << 3,
        All = First | Second | Third | Fourth,
    }

    public enum StandardEnum {
        Zero = 0,
        One = 1,
        TwoHundred = 200,
        NegTwoHundred = -200,
    }

    public string String { get; set; } = "A String";
    // public string MultiLineString { get; set; } = "A\nString"; // Fails to write to DB
    public string UnicodeString { get; set; } = "A ❤️";
    public string EmptyString { get; set; } = "";
    public string? NullString { get; set; } = null;

    public int PositiveInteger { get; set; } = int.MaxValue / 2;
    public int NegativeInteger { get; set; } = int.MinValue / 2;
    public int ZeroInteger { get; set; } = 0;
    public int MaxInteger { get; set; } = int.MaxValue;
    public int MinInteger { get; set; } = int.MinValue;

    public long PositiveLong { get; set; } = long.MaxValue / 2;
    public long NegativeLong { get; set; } = long.MinValue / 2;
    public long ZeroLong { get; set; } = 0;
    public long MaxLong { get; set; } = long.MaxValue;
    public long MinLong { get; set; } = long.MinValue;

    public float PositiveFloat { get; set; } = float.MaxValue / 7;
    public float NegativeFloat { get; set; } = float.MinValue / 7;
    public float ZeroFloat { get; set; } = 0;
    public float MaxFloat { get; set; } = float.MaxValue;
    public float MinFloat { get; set; } = float.MinValue;
    public float NaNFloat { get; set; } = float.NaN; // Not Supported by default by System.Text.Json
    public float EpsilonFloat { get; set; } = float.Epsilon;
    public float NegEpsilonFloat { get; set; } = -float.Epsilon;
    public float PositiveInfinityFloat { get; set; } = float.PositiveInfinity; // Not Supported by default by System.Text.Json
    public float NegativeInfinityFloat { get; set; } = float.NegativeInfinity; // Not Supported by default by System.Text.Json

    public double PositiveDouble { get; set; } = double.MaxValue / 7;
    public double NegativeDouble { get; set; } = double.MinValue / 7;
    public double ZeroDouble { get; set; } = 0;
    public double MaxDouble { get; set; } = double.MaxValue;
    public double MinDouble { get; set; } = double.MinValue;
    public double NaNDouble { get; set; } = double.NaN; // Not Supported by default by System.Text.Json
    public double EpsilonDouble { get; set; } = double.Epsilon;
    public double NegEpsilonDouble { get; set; } = -double.Epsilon;
    public double PositiveInfinityDouble { get; set; } = double.PositiveInfinity; // Not Supported by default by System.Text.Json
    public double NegativeInfinityDouble { get; set; } = double.NegativeInfinity; // Not Supported by default by System.Text.Json

    public decimal PositiveDecimal { get; set; } = decimal.MaxValue / 7m;
    public decimal NegativeDecimal { get; set; } = decimal.MinValue / 7m;
    public decimal ZeroDecimal { get; set; } = 0;
    public decimal MaxDecimal { get; set; } = decimal.MaxValue;
    public decimal MinDecimal { get; set; } = decimal.MinValue;

    public DateTime MaxUtcDateTime { get; set; } = DateTime.MaxValue.AsUtc(); // This fails to roundtrip, the fractions part of the date gets 00 prepended to it
    public DateTime MinUtcDateTime { get; set; } = DateTime.MinValue.AsUtc();
    public DateTime NowUtcDateTime { get; set; } = DateTime.Now.AsUtc();

    public Guid Guid { get; set; } = Guid.NewGuid();
    public Guid EmptyGuid { get; set; } = Guid.Empty;

    public bool TrueBool { get; set; } = true;
    public bool FalseBool { get; set; } = false;

    public StandardEnum ZeroStandardEnum { get; set; } = StandardEnum.Zero;
    public StandardEnum OneStandardEnum { get; set; } = StandardEnum.One;
    public StandardEnum TwoHundredStandardEnum { get; set; } = StandardEnum.TwoHundred;
    public StandardEnum NegTwoHundredStandardEnum { get; set; } = StandardEnum.NegTwoHundred;

    public FlagsEnum NoneFlagsEnum { get; set; } = FlagsEnum.None;
    public FlagsEnum AllFlagsEnum { get; set; } = FlagsEnum.All;
    public FlagsEnum SecondFourthFlagsEnum { get; set; } = FlagsEnum.Second | FlagsEnum.Fourth;
    public FlagsEnum UndefinedFlagsEnum { get; set; } = (FlagsEnum)(1 << 8);

    public static void AssertAreEqual(
        RoundTripObject a,
        RoundTripObject b) {
        Assert.NotNull(a);
        Assert.NotNull(b);

        b.Should().BeEquivalentTo(a);
    }
}