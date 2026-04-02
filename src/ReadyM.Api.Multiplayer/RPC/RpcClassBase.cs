using ReadyM.Api.DI;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Serialization;

namespace ReadyM.Api.Multiplayer.RPC;

public abstract class RpcClassBase(IRpcClient client, IRelaySerializer serializer) : IHostedService
{
    protected abstract byte EventsCount { get; }
    protected abstract void InitRpc();
    protected abstract void DeInitRpc();
    protected internal byte Offset { get; private set; }
    protected IRpcClient RelayClient { get; } = client;
    protected IRelaySerializer Serializer { get; } = serializer;

    internal void SetUpOffset(RpcOffsetProvider offsetProvider)
    {
        Offset = offsetProvider.GetNextOffset(EventsCount);
    }

    public virtual void OnScopeStart()
    {
        InitRpc();
    }

    public void Dispose()
    {
        DeInitRpc();
    }
}