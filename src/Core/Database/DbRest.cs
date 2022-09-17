using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rustic;
using Rustic.Text;

namespace Surreal.Net.Database;

public sealed class DbRest : ISurrealDatabase<SurrealRestResponse>, IDisposable
{
    private readonly HttpClient _client = new();
    private SurrealConfig _config;

    private static readonly Task<SurrealRestResponse> s_completed = Task.FromResult<SurrealRestResponse>(default);

    public Dictionary<string, object?> UseVariables { get; } = new();

    public JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonLowerSnakeCaseNamingPolicy.Instance,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        DictionaryKeyPolicy = JsonLowerSnakeCaseNamingPolicy.Instance,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public SurrealConfig GetConfig() => _config;

    public Task Open(SurrealConfig config, CancellationToken ct = default)
    {
        config.ThrowIfInvalid();
        _config = config;

        ConfigureClients();

        // Authentication
        _client.BaseAddress = config.RestEndpoint;
        SetAuth(config.Username, config.Password);

        // Use database
        SetUse(config.Database, config.Namespace);

        return Task.CompletedTask;
    }

    private void ConfigureClients()
    {
        _client.DefaultRequestHeaders.ConnectionClose = false;
    }

    private void SetAuth(string? user, string? pass)
    {
        // TODO: Support jwt auth
        _config.Username = user;
        _config.Password = pass;
        AuthenticationHeaderValue header = new("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}")));
        _client.DefaultRequestHeaders.Authorization = header;
    }

    private void RemoveAuth()
    {
        _config.Username = null;
        _config.Password = null;
        _client.DefaultRequestHeaders.Authorization = null;
    }

    private void SetUse(string? db, string? ns)
    {
        _config.Database = db;
        _config.Namespace = ns;
        _client.DefaultRequestHeaders.Add("DB", db);
        _client.DefaultRequestHeaders.Add("NS", ns);
    }

    public Task Close(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<SurrealRestResponse> Info(CancellationToken ct = default)
    {
        return Query("SELECT * FROM $auth", null, ct);
    }

    public Task<SurrealRestResponse> Use(string db, string ns, CancellationToken ct = default)
    {
        SetUse(db, ns);

        return s_completed;
    }

    public async Task<SurrealRestResponse> Signup(SurrealAuthentication auth, CancellationToken ct = default)
    {
        StringContent content = new(ToJson(auth), Encoding.UTF8, "application/json");
        return await Signup(content, ct);
    }

    /// <inheritdoc cref="Signup(SurrealAuthentication, CancellationToken)"/>
    public async Task<SurrealRestResponse> Signup(HttpContent auth, CancellationToken ct = default)
    {
        var rsp = await _client.PostAsync("signup", auth, ct);
        return await rsp.ToSurreal();
    }

    public async Task<SurrealRestResponse> Signin(SurrealAuthentication auth, CancellationToken ct = default)
    {
        StringContent content = new(ToJson(auth), Encoding.UTF8, "application/json");
        return await Signin(content, ct);
    }

    /// <inheritdoc cref="Signin(SurrealAuthentication, CancellationToken)"/>
    public async Task<SurrealRestResponse> Signin(HttpContent auth, CancellationToken ct = default)
    {
        var rsp = await _client.PostAsync("signin", auth, ct);
        return await rsp.ToSurreal();
    }

    public Task<SurrealRestResponse> Invalidate(CancellationToken ct = default)
    {
        SetUse(null, null);
        RemoveAuth();

        return s_completed;
    }

    public Task<SurrealRestResponse> Authenticate(string token, CancellationToken ct = default)
    {
        throw new NotSupportedException(); // TODO: Is it tho???
    }

    public Task<SurrealRestResponse> Let(string key, object? value, CancellationToken ct = default)
    {
        UseVariables[key] = value;
        return s_completed;
    }

    public async Task<SurrealRestResponse> Query(string sql, IReadOnlyDictionary<string, object?>? vars, CancellationToken ct = default)
    {
        string query = FormatVars(sql, vars);
        StringContent content = new(query, Encoding.UTF8, "application/json");
        return await Signin(content, ct);
    }

    /// <inheritdoc cref="Query(string, IReadOnlyDictionary{string, object?}?, CancellationToken)"/>
    public async Task<SurrealRestResponse> Query(HttpContent sql, CancellationToken ct = default)
    {
        var rsp = await _client.PostAsync("sql", sql, ct);
        return await rsp.ToSurreal();
    }

    public async Task<SurrealRestResponse> Select(SurrealThing thing, CancellationToken ct = default)
    {
        var rsp = await _client.GetAsync($"key/{FormatUrl(thing)}", ct);
        return await rsp.ToSurreal();
    }

    public async Task<SurrealRestResponse> Create(SurrealThing thing, object data, CancellationToken ct = default)
    {
        StringContent content = new(ToJson(data), Encoding.UTF8, "application/json");
        return await Create(thing, content, ct);
    }

    public async Task<SurrealRestResponse> Create(SurrealThing thing, HttpContent data, CancellationToken ct = default)
    {
        var rsp = await _client.PostAsync($"key/{FormatUrl(thing)}", data, ct);
        return await rsp.ToSurreal();
    }


    public async Task<SurrealRestResponse> Update(SurrealThing thing, object data, CancellationToken ct = default)
    {
        StringContent content = new(ToJson(data), Encoding.UTF8, "application/json");
        return await Update(thing, content, ct);
    }

    public async Task<SurrealRestResponse> Update(SurrealThing thing, HttpContent data, CancellationToken ct = default)
    {
        var rsp = await _client.PutAsync($"key/{FormatUrl(thing)}", data, ct);
        return await rsp.ToSurreal();
    }

    public async Task<SurrealRestResponse> Change(SurrealThing thing, object data, CancellationToken ct = default)
    {
        // Is this the most optimal way?
        string sql = "UPDATE $what MERGE $data RETURN AFTER";
        var vars = new Dictionary<string, object?>
        {
            ["what"] = thing.ToString(),
            ["data"] = data
        };
        return await Query(sql, vars, ct);
    }

    public async Task<SurrealRestResponse> Modify(SurrealThing thing, object data, CancellationToken ct = default)
    {
        StringContent content = new(ToJson(data), Encoding.UTF8, "application/json");
        return await Modify(thing, content, ct);
    }

    public async Task<SurrealRestResponse> Modify(SurrealThing thing, HttpContent data, CancellationToken ct = default)
    {
        var rsp = await _client.PatchAsync($"key/{FormatUrl(thing)}", data, ct);
        return await rsp.ToSurreal();
    }

    public async Task<SurrealRestResponse> Delete(SurrealThing thing, CancellationToken ct = default)
    {
        var rsp = await _client.DeleteAsync($"key/{FormatUrl(thing)}", ct);
        return await rsp.ToSurreal();
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private string FormatUrl(SurrealThing src, IReadOnlyDictionary<string, object?>? addVars = null)
    {
        using StrBuilder result = src.Length > 512 ? new(src.Length) : new(stackalloc char[src.Length]);
        if (!src.Table.IsEmpty)
            result.Append(Uri.EscapeDataString(src.Table.ToString()));
        if (!src.Table.IsEmpty && !src.Key.IsEmpty)
            result.Append('/');
        if (!src.Key.IsEmpty)
            result.Append(Uri.EscapeDataString(src.Key.ToString()));

        return FormatVars(result.ToString(), addVars);
    }

    private string FormatVars(string src, IReadOnlyDictionary<string, object?>? addVars = null)
    {
        if (!src.Contains('$'))
        {
            return src;
        }
        Dictionary<string, object?>? vars = addVars is null || addVars.Count == 0
            ? UseVariables
            : Combine(UseVariables, addVars);

        if (vars is null || vars.Count == 0)
            return src;

        // Serialize all objects
        foreach (var (k, v) in vars)
        {
            vars[k] = ToJson(v);
        }

        // Replace $vas with values for all variables, this doesnt support nesting
        NamedDef<object?> def = new("$", vars, null);
        return Fmt.Format(src, in def);
    }

    private string ToJson<T>(T? v)
    {
        return JsonSerializer.Serialize(v, SerializerOptions);
    }

    private static Dictionary<K, V>? Combine<K, V>(Dictionary<K, V>? lhs, IReadOnlyDictionary<K, V>? rhs)
        where K : notnull
    {
        // cheap way to combine two dictionaries
        if (lhs is null || lhs.Count == 0)
        {
            return rhs is null ? null : new(rhs);
        }
        if (rhs is null || rhs.Count == 0)
        {
            return lhs;
        }

        // the expensive way
        // org is the larger dictionary.  we'll add the smaller to it
        IReadOnlyDictionary<K, V> org = lhs;
        if (rhs.Count > lhs.Count)
        {
            org = rhs;
            rhs = lhs;
        }

        Dictionary<K, V> res = new(org);
        foreach (var (key, value) in rhs)
        {
            res[key] = value;
        }

        return res;
    }
}