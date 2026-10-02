using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.SDK.Archetypes;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Server;

internal sealed class EcsComponentsById(EcsApi ecs) : IComponentsById
{
    public bool Has<T>(int entityId) where T : struct, IComponent => ecs.HasComponent<T>(entityId);

    public ref T Ref<T>(int entityId) where T : struct, IComponent => ref ecs.GetComponentRef<T>(entityId);
}
