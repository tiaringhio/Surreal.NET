using System.Text.Json.Serialization;

namespace SurrealDB.Ws;

public struct WsError {
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
    public string? Message { get; set; }
}
