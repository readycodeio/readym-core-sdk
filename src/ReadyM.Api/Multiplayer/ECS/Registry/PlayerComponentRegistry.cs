using System.Collections.Generic;
using ReadyM.Api.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class PlayerComponentRegistry(IEnumerable<IPlayerComponentRegistration> registrations)
    : ArchetypeComponentRegistryBase<IPlayerComponentRegistry>(registrations), IPlayerComponentRegistry
{
    // empty
}
