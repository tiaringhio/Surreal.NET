using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using SurrealDB.Models;
using SurrealDB.Ws;

namespace SurrealDB.Driver.Rpc;

/// <summary>
///     The response from a query to the Surreal database via rpc.
/// </summary>
public readonly struct RpcResponse : IResponse {
    private readonly SurrealError _error;

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        RpcResponse(
            string id,
            SurrealError error,
            Result result) {
        Id = id;
        _error = error;
        UncheckedResult = result;
    }

    public string Id { get; }

    public bool IsOk => _error.Code == 0;
    public bool IsError => _error.Code != 0;

    public Result UncheckedResult { get; }
    public SurrealError UncheckedError => _error;

    public bool TryGetError(out SurrealError error) {
        error = _error;
        return IsError;
    }

    public bool TryGetResult(out Result result) {
        result = UncheckedResult;
        return IsOk;
    }

    public bool TryGetResult(
        out Result result,
        out SurrealError error) {
        result = UncheckedResult;
        error = _error;
        return IsOk;
    }

    public void Deconstruct(
        out Result result,
        out SurrealError error) {
        (result, error) = (UncheckedResult, _error);
    }

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        static RpcResponse From(in WsResponse rsp) {
        if (rsp.Id is null) {
            ThrowIdMissing();
        }

        if (rsp.Error.HasValue) {
            WsError err = rsp.Error.Value;
            return new(rsp.Id, new(err.Code, err.Message), default);
        }
        
        // SurrealDB likes to returns a list of one result. Unbox this response, to conform with the REST client
        Result res = Result.From(IntoSingle(rsp.Result));
        return new(rsp.Id, default, res);
    }

    public static JsonElement IntoSingle(in JsonElement root) {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() > 1) {
            return root;
        }
        
        var en = root.EnumerateArray();
        while (en.MoveNext()) {
            JsonElement cur = en.Current;
            // Return the first not null element
            if (cur.ValueKind is not JsonValueKind.Null or JsonValueKind.Undefined) {
                return cur;
            }
        }
        // No content in the array.
        return default;
    }
    
    [DoesNotReturn]
    private static void ThrowIdMissing() {
        throw new InvalidOperationException("Response does not have an id.");
    }
}