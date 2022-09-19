namespace Surreal.Net.Tests;

public class SurrealThingTests {

    private static readonly List<string> Keys = new() { "{0}ThingKey", "Thing{0}Key", "ThingKey{0}", "{0}" };
    private static readonly List<char> ComplexChars = new() { '-', ' ', '❤'};
    private static readonly List<char> StandardChars = new() { '_', '0'};

    public static List<object[]> StandardKeys => Keys.SelectMany(s => StandardChars.Select(c => string.Format(s, c))).Append("ThingKey").Select(e => new object[]{e}).ToList();
    public static List<object[]> ComplexKeys => Keys.SelectMany(s => ComplexChars.Select(c => string.Format(s, c))).Select(e => new object[]{e}).ToList();
    
    [Fact]
    public void TableSurrealThing() {
        var table = "TableName";

        var surrealThing = SurrealThing.From(table);

        Assert.Equal(table, surrealThing.ToString());
        Assert.Equal(table, surrealThing.Table.ToString());
    }
    
    [Theory]
    [MemberData(nameof(StandardKeys))]
    public void TableAndKeySurrealThing(string key) {
        var table = "TableName";
        var expectedThing = $"{table}:{key}";

        var surrealThing = SurrealThing.From(table, key);

        Assert.Equal(expectedThing, surrealThing.ToString());
        Assert.Equal(table, surrealThing.Table.ToString());
        Assert.Equal(key, surrealThing.Key.ToString());
    }
    
    [Theory]
    [MemberData(nameof(StandardKeys))]
    public void TableAndKeyFromStringSurrealThing(string key) {
        var table = "TableName";
        var expectedThing = $"{table}:{key}";

        var surrealThing = SurrealThing.From(expectedThing);

        Assert.Equal(expectedThing, surrealThing.ToString());
        Assert.Equal(table, surrealThing.Table.ToString());
        Assert.Equal(key, surrealThing.Key.ToString());
    }
    
    [Theory]
    [MemberData(nameof(ComplexKeys))]
    public void TableAndKeyWithComplexCharacterSurrealThing(string key) {
        var table = "TableName";
        var escapedKey = $"{SurrealThing.ComplexCharacterPrefix}{key}{SurrealThing.ComplexCharacterSuffix}";
        var expectedThing = $"{table}:{escapedKey}";

        var surrealThing = SurrealThing.From(table, key);

        Assert.Equal(expectedThing, surrealThing.ToString());
        Assert.Equal(table, surrealThing.Table.ToString());
        Assert.Equal(key, surrealThing.Key.ToString());
        Assert.Equal(escapedKey, surrealThing.RawKey.ToString());
    }
    
    [Theory]
    [MemberData(nameof(ComplexKeys))]
    public void TableAndKeyAlreadyEscapedSurrealThing(string key) {
        var table = "TableName";
        var escapedKey = $"{SurrealThing.ComplexCharacterPrefix}{key}{SurrealThing.ComplexCharacterSuffix}";
        var expectedThing = $"{table}:{escapedKey}";

        var surrealThing = SurrealThing.From(table, escapedKey);

        Assert.Equal(expectedThing, surrealThing.ToString());
        Assert.Equal(table, surrealThing.Table.ToString());
        Assert.Equal(key, surrealThing.Key.ToString());
        Assert.Equal(escapedKey, surrealThing.RawKey.ToString());
    }
}
