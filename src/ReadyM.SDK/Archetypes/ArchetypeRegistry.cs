using System.ComponentModel;

namespace ReadyM.SDK.Archetypes;

/// What each archetype gains from the mods that are loaded, which is what creating one carries.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ArchetypeRegistry
{
    private static readonly Dictionary<Type, List<ComponentSet>> Added = [];
    private static readonly Dictionary<Type, ComponentSet> Resolved = [];
    private static readonly object Gate = new();

    /// Called by generated code for each [Extends]. Safe to call more than once for the same pair.
    public static void Extend(Type archetype, ComponentSet components)
    {
        lock (Gate)
        {
            if (!Added.TryGetValue(archetype, out var sets))
                Added[archetype] = sets = [];

            if (!sets.Contains(components))
                sets.Add(components);

            Resolved.Remove(archetype);
        }
    }

    /// <summary>The archetype's own components plus everything added to it.</summary>
    internal static ComponentSet SetFor(Type archetype, ComponentSet own)
    {
        lock (Gate)
        {
            if (Resolved.TryGetValue(archetype, out var cached))
                return cached;

            if (!Added.TryGetValue(archetype, out var sets))
                return Resolved[archetype] = own;

            var all = new ComponentSet[sets.Count + 1];

            all[0] = own;
            sets.CopyTo(all, 1);

            return Resolved[archetype] = ComponentSet.Combine(all);
        }
    }
}
