using ReadyM.Api.Multiplayer.ECS.Jobs;
using ReadyM.Api.Multiplayer.ECS.Systems;

namespace ReadyM.Api.Multiplayer;

internal sealed class NetworkedComponentConfig(ReadyMultiplayerMod mod) : INetworkedComponentConfig
{
    private byte _nextComponentId;

    public INetworkedComponentConfig SynchronizeComponent<T>() where T : struct, INetworkedComponent
    {
        var id = new NetworkedComponentId(_nextComponentId++);

        mod.World.SystemRoot.Add(new SendClientComponentDeltaSystem<T>(id, mod.RelayClient));
        mod.DeltaReaderJobs.Add(id, new ApplyDeltaJob<T>(mod.NetManager));
        mod.SnapshotReaderJobs.Add(id, new ApplySnapshotJob<T>(mod.NetManager));

        return this;
    }
}