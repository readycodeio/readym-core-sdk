namespace ReadyM.Api.Saves;

/// <summary>
/// Indicates the layer of a save.
/// </summary>
public enum SaveLayer
{
    /// <summary>
    /// Indicates a world save, which contains the state of the world.
    /// </summary>
    WorldSave,

    /// <summary>
    /// Indicates a player save, which contains the equipment, inventory, skills, and other data related to a player.
    /// </summary>
    PlayerSave
}