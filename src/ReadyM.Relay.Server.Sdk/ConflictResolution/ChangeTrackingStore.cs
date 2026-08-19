using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Relay.Server.Sdk.Ecs;

namespace ReadyM.Relay.Server.Sdk.ConflictResolution;

public class ChangeTrackingStore(EcsApi ecs) : IChangeTrackingStore
{
    public ref T GetChangeComponent<T>(int id)
        where T : struct, IComponent
    {
        return ref ecs.GetComponentRef<T>(id);
    }
}
