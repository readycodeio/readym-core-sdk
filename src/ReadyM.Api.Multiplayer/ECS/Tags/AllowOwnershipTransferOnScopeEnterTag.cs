using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Tags;

/// <exclude/>
/// <summary>
/// Entities with this component get their ownership transferred to the first player that joins their scope,
/// if there are no other players in that scope when that player joins it.
/// </summary>
public readonly struct AllowOwnershipTransferOnScopeEnterTag : ITag;
