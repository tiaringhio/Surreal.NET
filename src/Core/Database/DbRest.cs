using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Surreal.Net.Database;

public sealed class DbRest : ISurrealDatabase, IDisposable
{
    private readonly HttpClient _client = new();
    private SurrealConfig _config;

    public SurrealConfig GetConfig() => _config;

    public Task Open(SurrealConfig config, CancellationToken ct = default)
    {
        config.ThrowIfInvalid();
        _config = config;

        ConfigureClients();

        // Authentication
        _client.BaseAddress = config.KeyEndpoint;
        SetAuth(config);

        // Use database
        SetUse(config.Database, config.Namespace);

        return Task.CompletedTask;
    }

    private void ConfigureClients()
    {
        _client.DefaultRequestHeaders.ConnectionClose = false;
    }

    private void SetAuth(SurrealConfig config)
    {
        AuthenticationHeaderValue header = new("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.Username}:{config.Password}")));
        _client.DefaultRequestHeaders.Authorization = header;
    }

    private void SetUse(string? db, string? ns)
    {
        _client.DefaultRequestHeaders.Add("DB", db);
        _client.DefaultRequestHeaders.Add("NS", ns);
    }

    public Task Close()
    {
        return Task.CompletedTask;
    }

    public Task<SurrealRpcResponse> Info()
    {
        throw new NotSupportedException();
    }

    public Task<SurrealRpcResponse> Use(string db, string ns, CancellationToken ct = default)
    {
        SetUse(db, ns);

        return Task.FromResult<SurrealRpcResponse>(default);
    }

    public Task<SurrealRpcResponse> Signup(SurrealAuthentication auth, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Signin(SurrealAuthentication auth, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Invalidate(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Authenticate(string token, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Let(string key, object? value, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Query(string sql, object? vars, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Select(SurrealThing thing, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Create(SurrealThing thing, object data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Update(SurrealThing thing, object data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Change(SurrealThing thing, object data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Modify(SurrealThing thing, object data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<SurrealRpcResponse> Delete(SurrealThing thing, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}