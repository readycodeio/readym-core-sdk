using ReadyM.Api.DI;
using ReadyM.Api.Multiplayer.Serialization;

namespace ReadyM.Relay.Server.Sdk.Rpc;

public abstract class ServerRpcHandlersBase(RpcApi rpc, IRelaySerializer serializer) : IHostedService
{
    protected RpcApi RpcApi { get; } = rpc;
    protected IRelaySerializer Serializer { get; } = serializer;

    protected abstract void InitRpc();
    protected abstract void DeInitRpc();

    public void OnScopeStart()
    {
        InitRpc();
    }

    public virtual void Dispose()
    {
        DeInitRpc();
        GC.SuppressFinalize(this);
    }
}