using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using SurrealDB.Common;
using SurrealDB.Json;
using SurrealDB.Models;
using SurrealDB.Ws;
namespace SurrealDB.Driver.Rpc;

internal static class RpcClientExtensions {

    internal static async Task<RpcResponse> ToSurreal(this Task<WsClient.Response> rsp) => ToSurreal(await rsp);
    internal static RpcResponse ToSurreal(this WsClient.Response rsp){
        if (rsp.id is null) {
            ThrowIdMissing();
        }
        
        if (rsp.error != default) {
            return new RpcResponse(new ErrorResult(rsp.error.code, string.Empty, rsp.error.message));
        }

        var result = UnpackFromStatusDocument(rsp.result);
        result = result.IntoSingle();

        // SurrealDB likes to returns a list of one result. Unbox this response, to conform with the REST client
        OkResult res = OkResult.From(result);
        return new(res);
    }

    [DoesNotReturn]
    private static void ThrowIdMissing() {
        throw new InvalidOperationException("Response does not have an id.");
    }

    private static JsonElement UnpackFromStatusDocument(in JsonElement root) {
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
                } else if (!resultStatusDocProperty.NameEquals("status")
                 && !resultStatusDocProperty.NameEquals("time")) {
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
}
