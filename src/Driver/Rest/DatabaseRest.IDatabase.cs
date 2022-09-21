using System.Collections.ObjectModel;

using SurrealDB.Abstractions;
using SurrealDB.Models;

namespace SurrealDB.Driver.Rest; 

public sealed partial class DatabaseRest : IDatabase {
    async Task<IResponse> IDatabase.Info(CancellationToken ct) {
        return await Info(ct);
    }

    async Task<IResponse> IDatabase.Use(string db, string ns, CancellationToken ct) {
        return await Use(db, ns, ct);
    }

    async Task<IResponse> IDatabase.Signup(Authentication auth, CancellationToken ct) {
        return await Signup(auth, ct);
    }

    async Task<IResponse> IDatabase.Signin(Authentication auth, CancellationToken ct) {
        return await Signin(auth, ct);
    }

    async Task<IResponse> IDatabase.Invalidate(CancellationToken ct) {
        return await Invalidate(ct);
    }

    async Task<IResponse> IDatabase.Authenticate(string token, CancellationToken ct) {
        return await Authenticate(token, ct);
    }

    async Task<IResponse> IDatabase.Let(string key, object? value, CancellationToken ct) {
        return await Let(key, value, ct);
    }

    async Task<IResponse> IDatabase.Query(string sql, ReadOnlyDictionary<string,object?>? vars, CancellationToken ct) {
        return await Query(sql, vars, ct);
    }

    async Task<IResponse> IDatabase.Select(Thing thing, CancellationToken ct) {
        return await Select(thing, ct);
    }

    async Task<IResponse> IDatabase.Create(Thing thing, object data, CancellationToken ct) {
        return await Create(thing, data, ct);
    }

    async Task<IResponse> IDatabase.Update(Thing thing, object data, CancellationToken ct) {
        return await Update(thing, data, ct);
    }

    async Task<IResponse> IDatabase.Change(Thing thing, object data, CancellationToken ct) {
        return await Change(thing, data, ct);
    }

    async Task<IResponse> IDatabase.Modify(Thing thing, object data, CancellationToken ct) {
        return await Modify(thing, data, ct);
    }

    async Task<IResponse> IDatabase.Delete(Thing thing, CancellationToken ct) {
        return await Delete(thing, ct);
    }
}