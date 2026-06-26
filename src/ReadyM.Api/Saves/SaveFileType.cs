namespace ReadyM.Api.Saves;

/// <summary>
/// Indicates the type of a save file.
/// </summary>
public enum SaveFileType
{
    /// <summary>
    /// Indicates a world save file, which contains the state of the world.
    /// </summary>
    WorldSave,

    /// <summary>
    /// Indicates a player save file, which contains the equipment, inventory, skills, and other data related to a player.
    /// </summary>
    PlayerSave
}