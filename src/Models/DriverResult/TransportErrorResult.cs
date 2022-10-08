namespace SurrealDB.Models.DriverResult;

public readonly record struct TransportErrorResult(int Code, string Status, string Detail);
