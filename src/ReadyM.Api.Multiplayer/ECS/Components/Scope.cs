namespace ReadyM.Api.Multiplayer.ECS.Components;

public enum Scope
{
    /// <summary>
    /// The entity is assigned to an area. It exists as long as the area exists. Information about the entity gets propagated
    /// to all players who are in the area. New players joining the area are sent the latest information as part of the
    /// area handshake snapshot. 
    /// </summary>
    Area,
    /// <summary>
    /// The entity is assigned to a player. It exists as long as the player is connected. Information about the entity gets
    /// propagated to all players inside the same area as the player. When the player is outside any area, the entity
    /// information is not propagated to other players. When the player joins another area, the entity information gets
    /// propagated to the players in that new area as part of a differential snapshot.
    /// </summary>
    Player,
    /// <summary>
    /// The entity is global. It exists forever until explicitly removed. The information about the entity is propagated
    /// to all connected players. When a new player connects, the entity information is sent to them as part of the
    /// global handshake snapshot.
    /// </summary>
    Global,
    /// <summary>
    /// The entity is only visible to the owner of the entity. It is not propagated to other players. This can be used
    /// to save information on the server. Private entities can also still be used for server-side logic.
    /// </summary>
    Private
}