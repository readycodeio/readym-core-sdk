using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

public class PlayerComponentRegistry(IEnumerable<IPlayerComponentRegistration> registrations)
    : ComponentRegistryBase<IPlayerComponentRegistry, IComponent>(registrations), IPlayerComponentRegistry
{
    public new void RegisterComponent<T>(T defaultValue = default) where T : struct, IComponent
    {
        base.RegisterComponent<T>(defaultValue);
    }
}
