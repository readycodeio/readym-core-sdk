using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Tags;

/// <exclude/>
/// <summary>
/// Entities with this tag keep their owner when that owner leaves their scope. The game system that sets it decides
/// who owns the entity next.
/// </summary>
internal readonly struct KeepOwnerOnScopeLeaveTag : ITag;
