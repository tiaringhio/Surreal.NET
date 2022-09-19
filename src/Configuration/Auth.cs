namespace SurrealDB.Configuration;

/// <summary>
///     Available authentication methods
/// </summary>
public enum Auth : byte {
    None = 0,
    Basic,
    JsonWebToken,
}