using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using ReadyM.SDK.Core;

namespace ReadyM.SDK.Client.Archetypes;

/// Runs <see cref="IArchetypeRegistry.ModifyArchetype"/> to collect components of mixins
/// that extend the built-in archetypes (World, Area, Cell, Player).
internal sealed class ArchetypeExtensionRegistration(
    DefaultWorldArchetypeRegistration world,
    DefaultAreaArchetypeRegistration area,
    DefaultPlayerArchetypeRegistration player,
    DefaultCellArchetypeRegistration cell,
    IEnumerable<IArchetypeShapeBindings> bindings,
    ILogger logger
) : IArchetypeRegistration
{
    public void Register(IArchetypeRegistry registry)
    {
        var bound = MapArchetypeIds();
        var applied = ClientArchetypes.ApplyExtensions(
            registry,
            shape => bound.TryGetValue(shape.FullName!, out var id) ? id : null
        );

        logger.LogDebug("Added {Count} component(s) from mod shapes to the game's archetypes", applied);
    }

    /// Maps full Archetype type names to the assigned ArchetypeId.
    private Dictionary<string, ArchetypeId> MapArchetypeIds()
    {
        var bound = new Dictionary<string, ArchetypeId>
        {
            [typeof(World).FullName!] = world.WorldArchetype,
            [typeof(Area).FullName!] = area.AreaArchetype,
            [typeof(Player).FullName!] = player.PlayerArchetype,
            [typeof(Cell).FullName!] = cell.CellArchetype
        };

        foreach (var binding in bindings)
        {
            foreach (var (shape, archetype) in binding.Bindings)
            {
                bound[shape] = archetype;
            }
        }

        return bound;
    }
}