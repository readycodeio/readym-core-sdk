using System.ComponentModel;
using ReadyM.Api.Idents;

namespace ReadyM.SDK.Client.Archetypes;

/// What archetype this game registered for each shape, collected as the archetypes are registered.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ArchetypeBindings
{
    private static IReadOnlyDictionary<string, ArchetypeId> _bound =
        new Dictionary<string, ArchetypeId>();

    internal static void Use(IReadOnlyDictionary<string, ArchetypeId> bound) => _bound = bound;

    internal static ArchetypeId? Of(Type shape)
        => shape.FullName is { } name && _bound.TryGetValue(name, out var id) ? id : null;
}
