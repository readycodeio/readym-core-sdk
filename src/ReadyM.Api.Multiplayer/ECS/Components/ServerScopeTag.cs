using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <summary>
/// Marks the server-only scope entity. Entities in it live on the server and are never replicated to clients.
/// </summary>
internal readonly struct ServerScopeTag : ITag;
