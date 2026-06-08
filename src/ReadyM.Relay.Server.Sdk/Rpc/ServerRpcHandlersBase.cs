using ReadyM.Api.DI;

namespace ReadyM.Relay.Server.Sdk.Rpc;

public abstract class ServerRpcHandlersBase(RpcApi rpc) : IHostedService
{
    protected RpcApi RpcApi { get; } = rpc;

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