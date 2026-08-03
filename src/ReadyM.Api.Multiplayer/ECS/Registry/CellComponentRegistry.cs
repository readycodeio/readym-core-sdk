using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class CellComponentRegistry(IEnumerable<ICellComponentRegistration> registrations)
    : ComponentRegistryBase<ICellComponentRegistry, IComponent>(registrations), ICellComponentRegistry
{
    public new void RegisterComponent<T>(T defaultValue = default) where T : struct, IComponent
    {
        base.RegisterComponent<T>(defaultValue);
    }
}
