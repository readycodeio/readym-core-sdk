using ReadyM.Api.DI;
using ReadyM.Api.Multiplayer.Serialization;

namespace ReadyM.Relay.Server.Sdk.Rpc;

public abstract class ServerRpcHandlersBase : IHostedService
{
    public RpcApi Rpc { protected get; set; }
    public IRelaySerializer Serializer { protected get; set; }

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