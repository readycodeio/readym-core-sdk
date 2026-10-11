using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>
/// Marks an entity that only its owning client deletes; it never changes owner or scope, and keeps this tag for life.
/// Every other entity is deleted only by the server.
/// </summary>
internal readonly struct ClientDeletableTag : ITag;
