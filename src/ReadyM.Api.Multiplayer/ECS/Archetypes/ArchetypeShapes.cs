using System;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

/// What the archetypes every game has are called in the SDK.
internal static class ArchetypeShapes
{
    public const string World = "ReadyM.SDK.Archetypes.Core.World";
    public const string Area = "ReadyM.SDK.Archetypes.Core.Area";
    public const string Player = "ReadyM.SDK.Archetypes.Core.Player";
    public const string Cell = "ReadyM.SDK.Archetypes.Core.Cell";

    public static string Of(WellKnownArchetype archetype) => archetype switch
    {
        WellKnownArchetype.World => World,
        WellKnownArchetype.Area => Area,
        WellKnownArchetype.Player => Player,
        WellKnownArchetype.Cell => Cell,
        _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null)
    };
}
