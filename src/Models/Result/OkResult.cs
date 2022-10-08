namespace SurrealDB.Models.DriverResult;

public readonly record struct OkResult(TimeSpan Time, string Status, ResultValue Value);
