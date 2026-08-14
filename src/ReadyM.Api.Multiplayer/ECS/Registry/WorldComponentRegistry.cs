using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class WorldComponentRegistry(IEnumerable<IWorldComponentRegistration> registrations)
    : ComponentRegistryBase<IWorldComponentRegistry, IComponent>(registrations), IWorldComponentRegistry
{
    public bool HasComponents => GetNextComponentId() > 0;

    public new void RegisterComponent<T>(T defaultValue = default) where T : struct, IComponent
    {
        base.RegisterComponent(defaultValue);
    }
}
