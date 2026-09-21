using ReadyM.SDK.Archetypes;

namespace ReadyM.SDK.Server;

/// Gives a mod's native collections their memory on entities the host builds itself.
/// Creating a shape runs this through <see cref="NativeInitRegistry"/>, but an entity of an
/// archetype the game registered is built by the host, which knows nothing of what a mod added
/// to that archetype with [Extends].
internal static class ExtensionNativeInit
{
    private static Type[]? _candidates;

    /// Called for every entity of an archetype no mod registered. Which components such an entity
    /// carries is not known here, so each candidate is checked against the entity; there are
    /// usually none at all, and the common case costs one null check.
    public static void Run(int entityId, IComponentsById components)
    {
        _candidates ??= Candidates();

        foreach (var component in _candidates)
            NativeInitRegistry.InitIfPresent(entityId, components, component);
    }

    /// Worked out once, after every mod has registered its shapes.
    private static Type[] Candidates()
    {
        var found = new List<Type>();

        foreach (var shape in ArchetypeContributions.Shapes())
        {
            foreach (var component in ArchetypeContributions.OwnGenerated(shape))
                if (NativeInitRegistry.Needs(component) && !found.Contains(component))
                    found.Add(component);

            foreach (var component in ArchetypeRegistry.GeneratedAddedTo(shape))
                if (NativeInitRegistry.Needs(component) && !found.Contains(component))
                    found.Add(component);
        }

        return [.. found];
    }
}
