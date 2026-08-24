using System.Collections.Generic;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class WorldComponentRegistry(IEnumerable<IWorldComponentRegistration> registrations)
    : ArchetypeComponentRegistryBase<IWorldComponentRegistry>(registrations), IWorldComponentRegistry
{
    // empty
}
