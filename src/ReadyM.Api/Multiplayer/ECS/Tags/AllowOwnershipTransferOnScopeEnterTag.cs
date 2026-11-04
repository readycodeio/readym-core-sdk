using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Tags;

/// <summary>
/// Hands the entity to the player whose game runs its area or cell: the first player into an empty one takes it, and
/// it follows when that player changes or the entity moves to another cell.
/// </summary>
public readonly struct AllowOwnershipTransferOnScopeEnterTag : ITag;
