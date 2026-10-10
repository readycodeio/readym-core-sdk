using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Marks an entity whose owner changed since the last send.</summary>
internal readonly struct OwnerChangedTag : ITag;
