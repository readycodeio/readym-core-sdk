using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Tags;

/// <summary>
/// The entity's owner keeps it after leaving its area or cell, and no player arriving takes it while the owner is
/// connected. It takes precedence over the two ownership transfer tags.
/// </summary>
public readonly struct KeepOwnerOnScopeLeaveTag : ITag;
