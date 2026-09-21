using System.ComponentModel;

namespace ReadyM.SDK.Archetypes;

/// What each archetype gains from the mods that are loaded, which is what creating one carries.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ArchetypeRegistry
{
    private static readonly Dictionary<Type, List<(Type Shape, ComponentSet Components)>> Added = [];
    private static readonly Dictionary<Type, ComponentSet> Resolved = [];

#if NET
    private static readonly Lock Gate = new();
#else
    private static readonly object Gate = new();
#endif

    /// Called by generated code for each [Extends]. Safe to call more than once for the same pair.
    /// <param name="shape">The shape that added them, which is what says whose components they are.</param>
    public static void Extend(Type archetype, Type shape, ComponentSet components)
    {
        lock (Gate)
        {
            if (!Added.TryGetValue(archetype, out var sets))
                Added[archetype] = sets = [];

            if (!sets.Contains((shape, components)))
                sets.Add((shape, components));

            Resolved.Remove(archetype);
        }
    }

    /// Only the added components the SDK generated.
    internal static IReadOnlyList<Type> GeneratedAddedTo(Type archetype)
    {
        lock (Gate)
        {
            if (!Added.TryGetValue(archetype, out var sets))
                return [];

            return
            [
                .. sets.SelectMany(entry => entry.Components.Types
                    .Where(component => component.Assembly == entry.Shape.Assembly))
            ];
        }
    }

    internal static IReadOnlyList<Type> Extended()
    {
        lock (Gate)
            return [.. Added.Keys];
    }

    internal static ComponentSet AddedTo(Type archetype)
    {
        lock (Gate)
        {
            return Added.TryGetValue(archetype, out var sets) && sets.Count > 0
                ? ComponentSet.Combine([.. sets.Select(entry => entry.Components)])
                : ComponentSet.Empty;
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

            for (var i = 0; i < sets.Count; i++)
                all[i + 1] = sets[i].Components;

            return Resolved[archetype] = ComponentSet.Combine(all);
        }
    }
}