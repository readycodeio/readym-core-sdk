using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal abstract class ArchetypeComponentRegistryBase<TRegistry>(IEnumerable<IComponentRegistrationBase<TRegistry, IComponent>> registrations)
    : ComponentRegistryBase<TRegistry, IComponent>(registrations), IArchetypeComponentRegistryBase<TRegistry>
    where TRegistry : IComponentRegistryBase<TRegistry, IComponent>
{
    public void RegisterComponent<T>(T defaultValue = default) where T : struct, IComponent
    {
        base.RegisterComponentImpl<T>(defaultValue);
    }
}
