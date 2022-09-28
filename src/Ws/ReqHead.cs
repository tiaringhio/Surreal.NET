using System.Text.Json;

namespace SurrealDB.Ws;

internal readonly record struct ReqHead(string? id, bool async, string? method) {
    /// <summary>
    /// Parses the head including the result propertyname, excluding the result array.
    /// </summary>
    public static (ReqHead head, int off, string? err) Parse(in ReadOnlySpan<byte> s) {
        Utf8JsonReader j = new(s, false, new JsonReaderState(new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }));

        if (!j.Read() || j.TokenType != JsonTokenType.StartObject) {
            return (default, default, "Unable to read token StartObject");
        }

        if (!j.Read() || j.TokenType != JsonTokenType.PropertyName
         || j.GetString() != "id") {
            return (default, default, "Unable to read PropertyName `id`");
        }

        if (!j.Read() || j.TokenType != JsonTokenType.String) {
            return (default, default, "Unable to read `id` value");
        }

        string? id = j.GetString();

        if (!j.Read() || j.TokenType != JsonTokenType.PropertyName
         || j.GetString() != "async") {
            return (default, default, "Unable to read PropertyName `async`");
        }

        if (!j.Read() || j.TokenType is not JsonTokenType.True and not JsonTokenType.False) {
            return (default, default, "Unable to read `async` value");
        }

        bool async = j.GetBoolean();

        if (!j.Read() || j.TokenType != JsonTokenType.PropertyName
         || j.GetString() != "method") {
            return (default, default, "Unable to read PropertyName `method`");
        }

        if (!j.Read() || j.TokenType is not JsonTokenType.True and not JsonTokenType.False) {
            return (default, default, "Unable to read `method` value");
        }

        string? method = j.GetString();

        if (!j.Read() || j.TokenType != JsonTokenType.PropertyName
         || j.GetString() != "result") {
            return (default, default, "Unable to read PropertyName `result`");
        }

        return (new() { id = id, async = async, method = method }, j.Position.GetInteger(), default);
    }

}
