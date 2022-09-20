using SurrealDB.Ws;

namespace SurrealDB.Driver.Rpc;

public static class RpcClientExtensions {
    public static RpcResponse ToSurreal(this WsResponse rsp) => RpcResponse.From(in rsp);

    public static async Task<RpcResponse> ToSurreal(this Task<WsResponse> rsp) => RpcResponse.From(await rsp);
}