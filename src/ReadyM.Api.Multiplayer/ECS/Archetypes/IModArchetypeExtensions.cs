using System.Collections.Generic;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

/// What the mods have added to an archetype the server owns.
/// <exclude/>
public interface IModArchetypeExtensions
{
    IReadOnlyList<int> For(WellKnownArchetype archetype);
}

/// Used until a mod host is up, and in any host that has none.
/// <exclude/>
public sealed class NoModArchetypeExtensions : IModArchetypeExtensions
{
    public IReadOnlyList<int> For(WellKnownArchetype archetype) => [];
}
