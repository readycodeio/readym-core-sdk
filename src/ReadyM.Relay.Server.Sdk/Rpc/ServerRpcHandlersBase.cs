using System.ComponentModel;
using ReadyM.Api.DI;
using ReadyM.Api.Multiplayer.Serialization;

namespace ReadyM.Relay.Server.Sdk.Rpc;

/// <exclude />
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class ServerRpcHandlersBase : IHostedService
{
    public RpcApi Rpc { protected get; set; }
    public IRelaySerializer Serializer { protected get; set; }

    /// <summary>
    /// Where entity parameters are looked up when a message arrives, set by the host. A ReadyM.SDK type this assembly
    /// cannot name, so it is held as an object.
    /// </summary>
    internal object? RpcEntities { get; set; }

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