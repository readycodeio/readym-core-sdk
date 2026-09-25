using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Interop;

/// <exclude/>
public delegate void TickSystemsDelegate(float deltaTime, float totalTime);

/// <exclude/>
///<remarks>
/// Called by the host right after it creates an entity, so the mod can run <c>INativeInit.Init</c> for its own components.
/// </remarks>
public delegate void PostCreateEntityInitDelegate(ArchetypeId archetype, RawEntity entity, byte local);

/// <exclude/>
///<remarks>
/// Called by the host just before it deletes an entity, so the mod can run what its shapes asked to
/// run as one goes. Unlike creation, this runs for every entity, replicated ones included.
/// </remarks>
public delegate void EntityDeletedDelegate(RawEntity entity);
