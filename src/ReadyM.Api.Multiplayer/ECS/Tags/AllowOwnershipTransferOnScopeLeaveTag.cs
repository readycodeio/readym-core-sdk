using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Tags;

/// <exclude/>
/// <summary>
/// Entities with this tag are transferred to another player when their owner leaves their scope in that scope or to
/// server if there are no more players left and that scope. Entities not marked this way are destroyed.
/// </summary>
public readonly struct AllowOwnershipTransferOnScopeLeaveTag : ITag;
