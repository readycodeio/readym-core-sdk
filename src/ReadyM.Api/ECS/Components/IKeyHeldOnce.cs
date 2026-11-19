namespace ReadyM.Api.ECS.Components;

/// <summary>
/// On an indexed component whose key is held once in the whole world (a player id), not once per dimension (an area,
/// a cell). Its shapes can then be looked up with no dimension.
/// </summary>
public interface IKeyHeldOnce;
