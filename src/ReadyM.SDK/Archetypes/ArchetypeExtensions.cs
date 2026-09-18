namespace ReadyM.SDK.Archetypes;

/// <summary>
/// What a shape can do, for code that holds one as a type parameter.
/// </summary>
public static class ArchetypeExtensions
{
    /// Whether the entity behind the shape is still in the world.
    public static bool IsValid<T>(this T shape) where T : struct, IArchetypeQueryable
        => shape.Handle.IsAlive();
}
