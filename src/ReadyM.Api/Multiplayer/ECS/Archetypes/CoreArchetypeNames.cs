namespace ReadyM.Api.Multiplayer.ECS.Archetypes;

/// <summary>
/// Stable names of the core archetypes. They are part of the save file format, so they must never change.
/// </summary>
internal static class CoreArchetypeNames
{
    public const string World = "World";
    public const string Area = "Area";
    public const string Cell = "Cell";
    public const string Player = "Player";
}
