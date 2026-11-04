using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Tags;

/// <summary>
/// When the entity's owner leaves its area or cell, hands it to another player there, or to the server if nobody is
/// left. Without this tag, or <see cref="KeepOwnerOnScopeLeaveTag"/>, the entity is deleted instead.
/// </summary>
public readonly struct AllowOwnershipTransferOnScopeLeaveTag : ITag;
