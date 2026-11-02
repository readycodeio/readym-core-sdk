using ReadyM.SDK.Archetypes.Core;

namespace ReadyM.SDK.Archetypes;

internal static class ArchetypeContributions
{
    /// <summary>
    /// The shapes the SDK declares for every game. They contribute even when nothing
    /// extended them. An archetype with no values of its own still has a marker, and without it a
    /// query for the shape finds nothing.
    /// </summary>
    private static readonly Type[] Core = [typeof(World), typeof(Area), typeof(Player), typeof(Cell)];

    public static IEnumerable<Type> Shapes()
        => Core.Concat(ArchetypeRegistry.Declared()).Concat(ArchetypeRegistry.Extended()).Distinct();

    /// <summary>
    /// Every declared shape that replicates, by full name: the order both sides register the ones their game does
    /// not bind in, so they hand out the same ids whatever order the assemblies loaded in.
    /// </summary>
    public static IEnumerable<(Type Shape, string Name, ComponentSet Components)> Replicated()
    {
        foreach (var shape in ArchetypeRegistry.Declared().OrderBy(shape => shape.FullName, StringComparer.Ordinal))
        {
            if (shape.FullName is not { } name || Activator.CreateInstance(shape) is not IArchetypeQueryable queryable)
                continue;

            var components = ArchetypeRegistry.SetFor(shape, queryable.Components);

            if (components.Replicated)
                yield return (shape, name, components);
        }
    }

    /// <summary>The components the SDK generated for a shape.</summary>
    public static IEnumerable<Type> OwnGenerated(Type shape)
    {
        if (Activator.CreateInstance(shape) is not IArchetypeQueryable queryable)
            yield break;

        foreach (var component in queryable.Components.Types)
            if (component.Assembly == shape.Assembly)
                yield return component;
    }
}