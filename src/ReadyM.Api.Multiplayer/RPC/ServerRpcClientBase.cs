using System;
using ReadyM.Api.DI;

namespace ReadyM.Api.Multiplayer.RPC;

public abstract class ServerRpcClientBase : IHostedService
{
    protected abstract void InitRpc();
    protected abstract void DeInitRpc();

    public virtual void OnScopeStart()
    {
        InitRpc();
    }

    public virtual void Dispose()
    {
        DeInitRpc();
        GC.SuppressFinalize(this);
    }
}