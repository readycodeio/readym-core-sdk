using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

public class AreaComponentRegistry(IEnumerable<IAreaComponentRegistration> registrations)
    : ComponentRegistryBase<IAreaComponentRegistry, IComponent>(registrations), IAreaComponentRegistry
{
    public new void RegisterComponent<T>(T defaultValue = default) where T : struct, IComponent
    {
        base.RegisterComponent<T>(defaultValue);
    }
}
