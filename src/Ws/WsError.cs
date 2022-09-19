using System.Text.Json.Serialization;

namespace SurrealDB.Ws;

#if SURREAL_NET_INTERNAL
public
#else
internal
#endif
    struct WsError {
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault),]
    public string? Message { get; set; }
}
