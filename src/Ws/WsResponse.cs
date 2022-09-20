using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDB.Ws;

public struct WsResponse {
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("error"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
    public WsError? Error { get; set; }

    [JsonPropertyName("result"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
    public JsonElement Result { get; set; }
}
