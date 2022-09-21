using System.Text.Json.Serialization;

namespace SurrealDB.Ws;

public struct WsNotify {
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("params"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
    public List<object?>? Params { get; set; }
}
