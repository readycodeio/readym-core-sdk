using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Archetypes;

/// <summary>
/// What a shape can do, for code that holds one as a type parameter.
/// </summary>
public static class ArchetypeExtensions
{
    /// <summary>Whether the entity behind the shape is still in the world.</summary>
    public static bool IsValid<T>(this T shape) where T : struct, IArchetypeQueryable
        => shape.Handle.IsAlive();

    /// <summary>The handle behind a shape, without boxing it.</summary>
    public static EntityHandle HandleOf<T>(this T shape) where T : struct, IArchetypeQueryable
        => shape.Handle;
}
