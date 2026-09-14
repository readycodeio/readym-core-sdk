using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Archetypes;

public static class Archetype
{
    public static EntityHandle HandleOf<T>(in T archetype) where T : struct, IArchetype
        => archetype.Handle;
}