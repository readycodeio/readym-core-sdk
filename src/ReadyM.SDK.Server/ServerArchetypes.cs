using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Core;
using HostArchetypes = ReadyM.Relay.Server.Sdk.Ecs.Components.ArchetypeRegistry;

namespace ReadyM.SDK.Server;

/// Tells the server what a mod's shapes add to the archetypes it owns.
public static class ServerArchetypes
{
    private static readonly (Type Shape, WellKnownArchetype Archetype)[] Owned =
    [
        (typeof(World), WellKnownArchetype.World),
        (typeof(Area), WellKnownArchetype.Area),
        (typeof(Player), WellKnownArchetype.Player),
        (typeof(Cell), WellKnownArchetype.Cell)
    ];

    /// Returns how many components were handed over, which a mod can log to see its shapes arrived.
    public static int ApplyExtensions(IArchetypeRegistry registry)
    {
        if (registry is not HostArchetypes host)
            return 0;

        var applied = 0;

        foreach (var (shape, archetype) in Owned)
        {
            var added = ArchetypeRegistry.AddedTo(shape);

            if (added.Count == 0)
                continue;

            host.AddArchetypeExtensions(archetype, added.Types);
            applied += added.Count;
        }

        return applied;
    }
}
