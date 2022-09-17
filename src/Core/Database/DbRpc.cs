namespace Surreal.Net.Database;


public sealed class DbRpc : ISurrealDatabase<SurrealRpcResponse>
{
    private readonly JsonRpcClient _client = new();
    private SurrealConfig _config;

    /// <inheritdoc />
    public SurrealConfig GetConfig() => _config;

    /// <inheritdoc />
    public async Task Open(SurrealConfig config, CancellationToken ct = default)
    {
        config.ThrowIfInvalid();
        _config = config;

        // Open connection
        InvalidConfigException.ThrowIfNull(config.RpcEndpoint);
        await _client.Open(config.RpcEndpoint!, ct);

        // Authenticate
        await SetAuth(config.Username, config.Password, ct);

        // Use database
        await SetUse(config.Database, config.Namespace, ct);
    }

    private async Task SetUse(string? db, string? ns, CancellationToken ct)
    {
        _config.Database = db;
        _config.Namespace = ns;
        await Use(db, ns, ct);
    }

    private async Task SetAuth(string? user, string? pass, CancellationToken ct)
    {
        // TODO: Support jwt auth
        _config.Username = user;
        _config.Password = pass;
        await Signin(new() { Username = user, Password = pass }, ct);
    }

    public async Task Close(CancellationToken ct = default)
    {
        await _client.Close(ct);
    }

    /// <param name="ct"></param>
    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Info(CancellationToken ct)
    {
        return await _client.Send(new()
        {
            Method = "info",
        });
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Use(string? db, string? ns, CancellationToken ct = default)
    {
        var rsp = await _client.Send(new()
        {
            Method = "use",
            Params = new() { db, ns }
        }, ct);

        if (!rsp.Error.HasValue)
        {
            _config.Database = db;
            _config.Namespace = ns;
        }

        return rsp;
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Signup(SurrealAuthentication auth, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "signup",
            Params = new() { auth }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Signin(SurrealAuthentication auth, CancellationToken ct = default)
    {
        var rsp = await _client.Send(new()
        {
            Method = "signin",
            Params = new() { auth }
        }, ct);

        // TODO: Update auth
        return rsp;
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Invalidate(CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "invalidate",
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Authenticate(string token, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "authenticate",
            Params = new() { token }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Let(string key, object? value, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "let",
            Params = new() { key, value }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Query(string sql, IReadOnlyDictionary<string, object?>? vars, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "query",
            Params = new() { sql, vars }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Select(SurrealThing thing, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "select",
            Params = new() { thing.ToString() }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Create(SurrealThing thing, object data, CancellationToken ct = default)
    {
        var rpcReq = new RpcRequest()
        {
            Method = "create",
            Async = true,
            Params = new() { thing.ToString(), data }
        };
        var response = await _client.Send(rpcReq, ct);
        return response;
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Update(SurrealThing thing, object data, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "update",
            Params = new() { thing.ToString(), data }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Change(SurrealThing thing, object data, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "change",
            Params = new() { thing.ToString(), data }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Modify(SurrealThing thing, object data, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "modify",
            Params = new() { thing.ToString(), data }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<SurrealRpcResponse> Delete(SurrealThing thing, CancellationToken ct = default)
    {
        return await _client.Send(new()
        {
            Method = "delete",
            Params = new() { thing.ToString() }
        }, ct);
    }
}
