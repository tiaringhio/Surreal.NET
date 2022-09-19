using SurrealDB.Ws;

namespace SurrealDB.Driver.Rpc;

public static class RpcClientExtensions {
#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        static RpcResponse ToSurreal(this WsResponse rsp) => RpcResponse.From(in rsp);

#if SURREAL_NET_INTERNAL
    public
#else
    internal
#endif
        static async Task<RpcResponse> ToSurreal(this Task<WsResponse> rsp) => RpcResponse.From(await rsp);
}