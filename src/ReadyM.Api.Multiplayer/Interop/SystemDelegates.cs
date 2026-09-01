using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Interop;

/// <exclude/>
public delegate void TickSystemsDelegate(float deltaTime, float totalTime);

/// <exclude/>
///<remarks>
/// Called by the host right after it creates an entity, so the mod can run <c>INativeInit.Init</c> for its own components.
/// </remarks>
public delegate void PostCreateEntityInitDelegate(ArchetypeId archetype, int entityId);
