using System.Text.Json;

namespace Surreal.Net;

/// <summary>
/// Indicates a table or a specific record.
/// </summary>
/// <remarks>
/// `table_name:record_id`
/// </remarks>
public readonly struct SurrealThing
{
    private readonly int _split;
    public string Thing { get; }
    
    public ReadOnlySpan<char> Table => Thing.AsSpan(0, _split);
    public ReadOnlySpan<char> Key => Thing.AsSpan(_split + 1);
    
#if SURREAL_NET_INTERNAL
    public
#endif
        SurrealThing(int split, string thing)
    {
        _split = split;
        Thing = thing;
    }

    public override string ToString() => Thing;
    
    public static SurrealThing From(string thing) => new(thing.IndexOf(':'), thing);

    public static SurrealThing From(in ReadOnlySpan<char> table, in ReadOnlySpan<char> key) => new(table.Length, $"{table}:{key}");

    public static implicit operator SurrealThing(in string thing) => From(thing);

    public SurrealThing WithTable(in ReadOnlySpan<char> table)
    {
        int keyOffset = table.Length + 1;
        int chars = keyOffset + Key.Length;
        Span<char> builder = stackalloc char[chars];
        table.CopyTo(builder);
        builder[table.Length] = ':';
        Key.CopyTo(builder.Slice(keyOffset));
        return new(table.Length, builder.ToString());
    }

    public SurrealThing WithKey(in ReadOnlySpan<char> key)
    {
        int keyOffset = Table.Length + 1;
        int chars = keyOffset + key.Length;
        Span<char> builder = stackalloc char[chars];
        Table.CopyTo(builder);
        builder[Table.Length] = ':';
        key.CopyTo(builder.Slice(keyOffset));
        return new(Table.Length, builder.ToString());
    }
}

/// <summary>
/// The result from a query to the Surreal database. 
/// </summary>
public readonly struct SurrealResult
{
    private readonly SurrealResponse _response;
    private readonly SurrealError _error;

#if SURREAL_NET_INTERNAL
    public
#endif
        SurrealResult(SurrealError error, SurrealResponse response)
    {
        _error = error;
        _response = response;
    }

    public bool IsOk => _error.Code == 0;
    public bool IsError => _error.Code != 0;
    
    public SurrealResponse UncheckedResponse => _response;
    public SurrealError UncheckedError => _error;
    
    public bool TryGetError(out SurrealError error)
    {
        error = _error;
        return IsError;
    }
    
    public bool TryGetResult(out SurrealResponse result)
    {
        result = _response;
        return IsOk;
    }
    
    public bool TryGetResult(out SurrealResponse result, out SurrealError error)
    {
        result = _response;
        error = _error;
        return IsOk;
    }

    public void Deconstruct(out SurrealResponse result, out SurrealError error) => (result, error) = (_response, _error);
}

/// <summary>
/// The response from a query to the Surreal database.
/// </summary>
public readonly struct SurrealResponse
{
    private readonly SurrealResponseKind _kind;
    private readonly JsonElement _json;
    private readonly string? _id;
    private readonly string? _text;

#if SURREAL_NET_INTERNAL
    public
#endif
    SurrealResponse(SurrealResponseKind kind, JsonElement json, string? id, string? text)
    {
        _kind = kind;
        _json = json;
        _id = id;
        _text = text;
    }

    public SurrealResponseKind Kind => _kind;
    public JsonElement UncheckedJson => _json;
    public string? UncheckedId => _id;
    public string? UncheckedText => _text;
    
    public bool TryGetObject(out JsonElement json)
    {
        json = _json;
        return _kind is SurrealResponseKind.DocumentWithId or SurrealResponseKind.Object;
    }
    
    public bool TryGetDocumentWithId(out string? id, out JsonElement json)
    {
        id = _id;
        json = _json;
        return _kind is SurrealResponseKind.DocumentWithId;
    }
    
    public bool TryGetText(out string? text)
    {
        text = _text;
        return _kind is SurrealResponseKind.Text;
    }
}

/// <summary>
/// Indicates the type of response from the Surreal database.
/// </summary>
public enum SurrealResponseKind : byte
{
    None,
    Text,
    DocumentWithId,
    Object,
}

/// <summary>
/// The error from a query to the Surreal database.
/// </summary>
public readonly struct SurrealError
{
#if SURREAL_NET_INTERNAL
    public
#endif
    SurrealError(int code, string message)
    {
        Code = code;
        Message = message;
    }

    public int Code { get; }
    public string Message { get; }
}

public sealed class SurrealAuthentication
{
    public string? Namespace { get; set; }
    public string? Database { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

/// <summary>
/// Common interface for interacting with a SurrealDB instance
/// </summary>
public interface ISurrealClient
{
    /// <summary>
    /// Opens the connection to a SurrealDB instance using the provided configuration.
    /// Configures the client with all applicable settings.
    /// </summary>
    public Task Open(SurrealConfig config, CancellationToken ct = default);

    /// <summary>
    /// Returns a copy of the current configuration.
    /// </summary>
    public SurrealConfig GetConfig();

    /// <summary>
    /// Closes the open connection the the SurrealDB.
    /// </summary>
    public void Close();

    /// <summary>
    /// Retrieves the current session information.
    /// </summary>
    public Task<SurrealResult> Info();

    /// <summary>
    /// Switch to a specific namespace and database.
    /// </summary>
    /// <param name="db">Switches to a specific namespace.</param>
    /// <param name="ns">Switches to a specific database.</param>
    public Task<SurrealResult> Use(string db, string ns, CancellationToken ct = default);

    /// <summary>
    /// Signs up to a specific authentication scope.
    /// </summary>
    /// <param name="auth">Variables used in a signin query.</param>
    public Task<SurrealResult> Signup(SurrealAuthentication auth, CancellationToken ct = default);

    /// <summary>
    /// Signs in to a specific authentication scope.
    /// </summary>
    /// <param name="auth">Variables used in a signin query.</param>
    /// <remarks>
    /// This updates the internal <see cref="SurrealConfig"/>.
    /// </remarks>
    public Task<SurrealResult> Signin(SurrealAuthentication auth, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the authentication for the current connection.
    /// </summary>
    /// <remarks>
    /// This updates the internal <see cref="SurrealConfig"/>.
    /// </remarks>
    public Task<SurrealResult> Invalidate(CancellationToken ct = default);

    /// <summary>
    /// Authenticates the current connection with a JWT token.
    /// </summary>
    /// <param name="token"> The JWT authentication token.</param>
    /// <remarks>
    /// This updates the internal <see cref="SurrealConfig"/>.
    /// </remarks>
    public Task<SurrealResult> Authenticate(string token, CancellationToken ct = default);

    /// <summary>
    /// Assigns a value as a parameter for this connection.
    /// </summary>
    /// <param name="key">Specifies the name of the variable.</param>
    /// <param name="value">Assigns the value to the variable name.</param>
    public Task<SurrealResult> Let(string key, object? value, CancellationToken ct = default);

    /// <summary>
    /// Runs a set of SurrealQL statements against the database.
    /// </summary>#
    /// <param name="sql">Specifies the SurrealQL statements.</param>
    /// <param name="vars">Assigns variables which can be used in the query.</param>
    public Task<SurrealResult> Query(string sql, object? vars, CancellationToken ct = default);

    /// <summary>
    /// Selects all records in a table, or a specific record, from the database.
    /// </summary>
    /// <param name="thing"> The table name or a record id to select.</param>
    /// <remarks>
    /// This function will run the following query in the database:
    /// <code>SELECT * FROM $thing;</code>
    /// </remarks>
    public Task<SurrealResult> Select(SurrealThing thing, CancellationToken ct = default);

    /// <summary>
    /// Creates a record in the database.
    /// </summary>
    /// <param name="thing"> The table name or the specific record id to create. </param>
    /// <param name="data"> The document / record data to insert. </param>
    /// <remarks>
    /// This function will run the following query in the database:
    /// <code>CREATE $thing CONTENT $data;</code>
    /// </remarks>
    public Task<SurrealResult> Create(SurrealThing thing, object data, CancellationToken ct = default);

    /// <summary>
    /// Updates all records in a table, or a specific record, in the database.
    /// </summary>
    /// <param name="thing"> The table name or the specific record id to update. </param>
    /// <param name="data"> The document / record data to insert. </param>
    /// <remarks>
    /// This function replaces the current document / record data with the specified data.
    ///
    /// This function will run the following query in the database:
    /// <code>UPDATE $thing CONTENT $data;</code>
    /// </remarks>
    public Task<SurrealResult> Update(SurrealThing thing, object data, CancellationToken ct = default);

    /// <summary>
    /// Modifies all records in a table, or a specific record, in the database.
    /// </summary>
    /// <param name="thing"> The table name or the specific record id to update. </param>
    /// <param name="data"> The document / record data to insert. </param>
    /// <remarks>
    /// This function merges the current document / record data with the specified data.
    /// 
    /// This function will run the following query in the database:
    /// <code>UPDATE $thing MERGE $data;</code>
    /// </remarks>
    public Task<SurrealResult> Change(SurrealThing thing, object data, CancellationToken ct = default);

    /// <summary>
    /// Applies  <see href="https://jsonpatch.com/">JSON Patch</see> changes to all records, or a specific record, in the database.
    /// </summary>
    /// <param name="thing"> The table name or the specific record id to update. </param>
    /// <param name="data"> The JSON Patch data with which to modify the records. </param>
    /// <remarks>
    /// This function patches the current document / record data with the specified JSON Patch data.
    ///
    /// This function will run the following query in the database:
    /// <code>UPDATE $thing PATCH $data;</code>
    /// </remarks>
    public Task<SurrealResult> Modify(SurrealThing thing, object data, CancellationToken ct = default);

    /// <summary>
    /// Deletes all records in a table, or a specific record, from the database.
    /// </summary>
    /// <param name="thing"> The table name or a record id to select. </param>
    /// <remarks>
    /// This function will run the following query in the database:
    /// <code>DELETE * FROM $thing;</code>
    /// </remarks>
    public Task<SurrealResult> Delete(SurrealThing thing, CancellationToken ct = default);
}