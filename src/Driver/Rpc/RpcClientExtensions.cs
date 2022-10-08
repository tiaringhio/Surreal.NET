using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using SurrealDB.Common;
using SurrealDB.Driver.Rest;
using SurrealDB.Models;
using SurrealDB.Ws;
namespace SurrealDB.Driver.Rpc;

internal static class RpcClientExtensions {

    internal static async Task<DriverResponse> ToSurreal(this Task<WsClient.Response> rsp) => ToSurreal(await rsp);
    internal static DriverResponse ToSurreal(this WsClient.Response rsp){
        if (rsp.id is null) {
            ThrowIdMissing();
        }

        if (rsp.error != default) {
            return new DriverResponse(RawResult.TransportError(rsp.error.code, string.Empty, rsp.error.message ?? ""));
        }

        return UnpackFromStatusDocument(rsp.result);
    }

    private static DriverResponse UnpackFromStatusDocument(in JsonElement root) {
        // Some results come as a simple object or an array of objects or even and empty string
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
            return ToSingleUnknown(root);
        }

        foreach (var resultStatusDoc in root.EnumerateArray()) {
            if (resultStatusDoc.ValueKind != JsonValueKind.Object) {
                // if this was a status document, we would expect an object here
                return ToSingleUnknown(root);
            }

            var propertyCount = 0;
            JsonElement resultProperty = default; // ??
            foreach (var resultStatusDocProperty in resultStatusDoc.EnumerateObject()) {
                propertyCount++;
                if (resultStatusDocProperty.NameEquals("result")) {
                    resultProperty = resultStatusDocProperty.Value;
                } else if (!resultStatusDocProperty.NameEquals("status")
                 && !resultStatusDocProperty.NameEquals("time")) {
                    // this property is not part of the 'status document',
                    // at this point we can be confident that it is just a simple array of objects
                    // so lets just return it
                    return ToSingleUnknown(in root);
                }
            }

            if (propertyCount == 3) {
                // We ended up with 3 properties, so this must be a status document
                return FromArray(in root);
            }
        }

        // if we get here then all the properties had valid status document names
        // but was missing some of them
        return ToSingleUnknown(root);
    }

    private static DriverResponse ToSingleUnknown(in JsonElement element) {
        return new(RawResult.Unknown(element.IntoSingle()));
    }

    private static DriverResponse FromArray(in JsonElement element) {
        ArrayBuilder<RawResult> builder = new();
        foreach (JsonElement e in element.EnumerateArray()) {
            OkOrErrorResult res = e.Deserialize<OkOrErrorResult>();
            builder.Append(res.ToResult());
        }

        return DriverResponse.FromOwned(builder.AsSegment());
    }

    [DoesNotReturn]
    private static void ThrowIdMissing() {
        throw new InvalidOperationException("Response does not have an id.");
    }

}
