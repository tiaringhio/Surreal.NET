using Newtonsoft.Json;

namespace Surreal.NET.Models;

public class SurrealResult<T> where T : class
{
    [JsonProperty("time")]
    public string Time { get; set; }
    [JsonProperty("status")]
    public string Status { get; set; }
    [JsonProperty("result")]
    public IEnumerable<T> Result { get; set; }
}