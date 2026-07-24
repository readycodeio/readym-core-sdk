using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Tags;

/// <summary>
/// Entities with this tag are destroyed when their owner leaves their scope.
/// Other entities get their ownership transferred to another player in that scope or to server
/// if there are no players left and that scope is not deleted when all players leave.
/// </summary>
public readonly struct DisallowOwnershipTransferOnScopeLeaveTag : ITag;