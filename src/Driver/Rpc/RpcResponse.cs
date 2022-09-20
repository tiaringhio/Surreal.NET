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

public RpcResponse(
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

public static RpcResponse From(in WsResponse rsp) {
        if (rsp.Id is null) {
            ThrowIdMissing();
        }

        if (rsp.Error.HasValue) {
            WsError err = rsp.Error.Value;
            return new(rsp.Id, new(err.Code, err.Message), default);
        }

        var result = UnpackFromStatusDocument(rsp.Result);
        result = IntoSingle(result);
        
        // SurrealDB likes to returns a list of one result. Unbox this response, to conform with the REST client
        Result res = Result.From(IntoSingle(result));
        return new(rsp.Id, default, res);
    }

    public static JsonElement UnpackFromStatusDocument(in JsonElement root) {
        // Some results come as a simple array of objects (basically just a results array)
        // [ { }, { }, ... ]
        // Others come embedded into a 'status document' that can have multiple result sets
        //[
        //  {
        //    "result": [ { }, { }, ... ],
        //    "status": "OK",
        //    "time": "71.775µs"
        //  }
        //]

        if (root.ValueKind != JsonValueKind.Array) {
            return root;
        }

        foreach (var resultStatusDoc in root.EnumerateArray()) {
            if (resultStatusDoc.ValueKind != JsonValueKind.Object) {
                // if this was a status document, we would expect an object here
                return root;
            }

            var propertyCount = 0;
            JsonElement? resultProperty = null;
            foreach (var resultStatusDocProperty in resultStatusDoc.EnumerateObject()) {
                propertyCount++;
                if (resultStatusDocProperty.NameEquals("result")) {
                    resultProperty = resultStatusDocProperty.Value;
                }
                else if (!resultStatusDocProperty.NameEquals("status") && !resultStatusDocProperty.NameEquals("time")) {
                    // this property is not part of the 'status document',
                    // at this point we can be confident that it is just a simple array of objects
                    // so lets just return it
                    return root;
                }
            }

            if (propertyCount == 3) {
                // We ended up with 3 properties, so this must be a status document
                return resultProperty!.Value;
            }
        }

        // if we get here then all the properties had valid status document names
        // but was missing some of them
        return root;
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