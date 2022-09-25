namespace SurrealDB.Driver.Tests;

[CollectionDefinition("SurrealDBRequired")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Sets up the db proccess for all tests that are in the 'SurrealDBRequired' collection
///
/// To use:
/// - Add `[Collection("SurrealDBRequired")]` to the test class
/// - Add `TestDatabaseFixture? fixture;` field to the test class
/// </summary>
public class TestDatabaseFixture {
}
