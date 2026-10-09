namespace ReadyM.Api.Saves;

/// <summary>
/// Indicates the layer of a save: which kind of partition an entity is saved into.
/// </summary>
public enum SaveLayer
{
    /// <summary>
    /// The global world state: the World entity and everything not tied to an area, a cell or a player.
    /// </summary>
    Global = 0,

    /// <summary>
    /// Indicates a player save, which contains the equipment, inventory, skills, and other data related to a player.
    /// </summary>
    Player = 1,

    /// <summary>
    /// Area segment of the world save: the area scope and the entities directly in it.
    /// </summary>
    Area = 2,

    /// <summary>
    /// Cell segment of the world save: the cell scope and the entities in it.
    /// </summary>
    Cell = 3,
}
