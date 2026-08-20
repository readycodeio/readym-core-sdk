using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Relay.Server.Sdk.Ecs;

namespace ReadyM.Relay.Server.Sdk.ConflictResolution;

public class ChangeTrackingStore(EcsApi ecs) : IChangeTrackingStore
{
    public ref T GetChangeComponent<T>(int id)
        where T : struct, IComponent
    {
        if (!ecs.HasComponent<T>(id))
            throw new InvalidOperationException($"Missing change {typeof(T)} for entity {id}");

        return ref ecs.GetComponentRef<T>(id);
    }
}
