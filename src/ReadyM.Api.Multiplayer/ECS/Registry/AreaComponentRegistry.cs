using System.Collections.Generic;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class AreaComponentRegistry(IEnumerable<IAreaComponentRegistration> registrations)
    : ArchetypeComponentRegistryBase<IAreaComponentRegistry>(registrations), IAreaComponentRegistry
{
    // empty
}
