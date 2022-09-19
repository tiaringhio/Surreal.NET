using System.Text.Json.Serialization;

namespace SurrealDB.Models;

public sealed class Authentication {
    [JsonPropertyName("ns")]
    public string? Namespace { get; set; }
    [JsonPropertyName("db")]
    public string? Database { get; set; }
    [JsonPropertyName("sc")]
    public string? Scope { get; set; }
    [JsonPropertyName("user")]
    public string? Username { get; set; }
    [JsonPropertyName("pass")]
    public string? Password { get; set; }
}