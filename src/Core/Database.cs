namespace Surreal.Net;

public sealed class Database : ISurrealClient
{
    private readonly JsonRpcClient _client = new();
    private SurrealConfig _config;

    /// <inheritdoc />
    public SurrealConfig GetConfig() => _config;

    /// <inheritdoc />
    public async Task Open(SurrealConfig config, CancellationToken ct = default)
    {
        config.ThrowIfInvalid();
        // Open connection
        InvalidConfigException.ThrowIfNull(config.RpcUrl);
        await _client.Open(config.RpcUrl!, ct);

        // Authenticate
        await (config.Authentication switch
        {
            Auth.Basic => Signin(new() { Username = config.Username, Password = config.Password }, ct),
            Auth.JsonWebToken => Authenticate(config.JsonWebToken!, ct),
            _ => Task.CompletedTask
        });
        
        // Use database
        if (config.Database is null ^ config.Namespace is null)
        {
            InvalidConfigException.ThrowIfNull(config.Database);
            InvalidConfigException.ThrowIfNull(config.Namespace);
        }
        if (config.Database is not null && config.Namespace is not null)
        {
            await Use(config.Database, config.Namespace, ct);
        }
        
        _config = config;
    }

    /// <inheritdoc />
    public async Task Close()
    {
        await _client.Close();
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Info()
    {
        return await _client.Send(new()
        {
            Method = "info",
        });
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Use(string db, string ns, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "use",
            Params = new() { db, ns }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Signup(SurrealAuthentication auth, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "signup",
            Params = new() { auth }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Signin(SurrealAuthentication auth, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "signin",
            Params = new() { auth }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Invalidate(CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "invalidate",
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Authenticate(string token, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "authenticate",
            Params = new() { token }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Let(string key, object? value, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "let",
            Params = new() { key, value }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Query(string sql, object? vars, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "query",
            Params = new() { sql, vars }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Select(SurrealThing thing, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "select",
            Params = new() { thing.ToString() }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Create(SurrealThing thing, object data, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "create",
            Params = new() { thing.ToString(), data }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Update(SurrealThing thing, object data, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "update",
            Params = new() { thing.ToString(), data }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Change(SurrealThing thing, object data, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "change",
            Params = new() { thing.ToString(), data }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Modify(SurrealThing thing, object data, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "modify",
            Params = new() { thing.ToString(), data }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealResponse> Delete(SurrealThing thing, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "delete",
            Params = new() { thing.ToString() }
        }, ct);
    }
}