using System.Text.Json.Serialization;

namespace SurrealDB.Shared.Tests;

public class TestObject<TKey, TValue> {
    [JsonConstructor]
    public TestObject(TKey key, TValue value) {
        Key = key;
        Value = value;
    }

    public TKey Key { get; set; }
    public TValue Value { get; set; }
}

public class ExtendedTestObject<TKey, TValue> : TestObject<TKey, TValue> {
    [JsonConstructor]
    public ExtendedTestObject(TKey key, TValue value, TValue mergeValue) : base (key, value) {
        MergeValue = mergeValue;
    }
    
    public TValue MergeValue { get; set; }
}

public record SimpleSignupRequest(
    string user,
    string password,
    string NS,
    string DB,
    string SC) : SignupRequestBase(NS, DB, SC);

public record IdSignupRequest(
    string user,
    string password,
    string id,
    string NS,
    string DB,
    string SC) : SignupRequestBase(NS, DB, SC);
