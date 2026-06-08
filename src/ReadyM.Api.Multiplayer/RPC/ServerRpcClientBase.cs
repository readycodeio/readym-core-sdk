using System;
using ReadyM.Api.DI;
using ReadyM.Api.Multiplayer.Client;

namespace ReadyM.Api.Multiplayer.RPC;

public abstract class ServerRpcClientBase(IRpcClient relayClient) : IHostedService
{
    protected IRpcClient RelayClient = relayClient;
    
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