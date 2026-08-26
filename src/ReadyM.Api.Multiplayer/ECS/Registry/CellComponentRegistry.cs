using System.Collections.Generic;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class CellComponentRegistry(IEnumerable<ICellComponentRegistration> registrations)
    : ArchetypeComponentRegistryBase<ICellComponentRegistry>(registrations), ICellComponentRegistry
{
    // empty
}
