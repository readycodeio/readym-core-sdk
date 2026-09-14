namespace ReadyM.SDK;

public static class Archetype
{
    public static EntityHandle HandleOf<T>(in T archetype) where T : struct, IArchetype
        => archetype.Handle;
}