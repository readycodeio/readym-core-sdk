using System;

namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

/// What the archetypes every game has are called in the SDK.
internal static class ArchetypeShapes
{
    public const string World = "ReadyM.SDK.Core.World";
    public const string Area = "ReadyM.SDK.Core.Area";
    public const string Player = "ReadyM.SDK.Core.Player";
    public const string Cell = "ReadyM.SDK.Core.Cell";

    public static string Of(WellKnownArchetype archetype) => archetype switch
    {
        WellKnownArchetype.World => World,
        WellKnownArchetype.Area => Area,
        WellKnownArchetype.Player => Player,
        WellKnownArchetype.Cell => Cell,
        _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, null)
    };
}
