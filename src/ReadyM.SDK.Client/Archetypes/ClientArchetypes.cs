using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Client.Archetypes;

internal static class ClientArchetypes
{
    /// <summary>Returns how many components were added, which a game can log to see its shapes arrived.</summary>
    /// <param name="idOf">
    /// Which archetype a shape is, or null for one this game does not register. Null is ordinary:
    /// a shape one mod declared and another extended is built from its component set, which the
    /// SDK has already widened, so there is no archetype here to change.
    /// </param>
    public static int ApplyExtensions(IArchetypeRegistry registry, Func<Type, ArchetypeId?> idOf)
    {
        var applied = 0;

        foreach (var shape in ArchetypeContributions.Shapes())
        {
            if (idOf(shape) is not { } id)
                continue;

            var contributed = new List<Type>();

            // What the shape itself brought. An archetype with no values of its own still has a
            // marker, and without it on the entity a query for the shape finds nothing.
            contributed.AddRange(ArchetypeContributions.OwnGenerated(shape));

            // And what mods added with [Extends].
            contributed.AddRange(ArchetypeRegistry.GeneratedAddedTo(shape));

            if (contributed.Count == 0)
                continue;

            registry.ModifyArchetype(id, builder =>
            {
                foreach (var component in contributed)
                    builder.Add(component);
            });

            applied += contributed.Count;
        }

        return applied;
    }

    /// <summary>
    /// Registers an archetype under its full name for every replicated shape the game does not bind. The server
    /// does the same, so the two agree on the ids as long as they load the same shapes.
    /// </summary>
    /// <param name="bound">The archetype the game registered for each shape, by full name.</param>
    /// <returns>Each archetype registered, by the shape's full name.</returns>
    public static List<(string Shape, ArchetypeId Archetype)> RegisterReplicated(
        IArchetypeRegistry registry,
        IReadOnlyDictionary<string, ArchetypeId> bound)
    {
        var registered = new List<(string, ArchetypeId)>();

        foreach (var (_, name, components) in ArchetypeContributions.Replicated())
        {
            if (bound.ContainsKey(name))
                continue;

            var builder = new ArchetypeBuilder();

            foreach (var component in components.Types)
                builder.Add(component);

            registered.Add((name, registry.RegisterArchetype(name, builder)));
        }

        return registered;
    }
}
