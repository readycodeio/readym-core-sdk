using ReadyM.SDK.Core;

namespace ReadyM.SDK.Archetypes;

internal static class ArchetypeContributions
{
    /// The shapes the SDK declares for every game. They contribute even when nothing
    /// extended them. An archetype with no values of its own still has a marker, and without it a
    /// query for the shape finds nothing.
    private static readonly Type[] Core = [typeof(World), typeof(Area), typeof(Player), typeof(Cell)];

    public static IEnumerable<Type> Shapes() => Core.Concat(ArchetypeRegistry.Extended()).Distinct();

    /// The components the SDK generated for a shape.
    public static IEnumerable<Type> OwnGenerated(Type shape)
    {
        if (Activator.CreateInstance(shape) is not IArchetypeQueryable queryable)
            yield break;

        foreach (var component in queryable.Components.Types)
            if (component.Assembly == shape.Assembly)
                yield return component;
    }
}
