using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Marks an entity created since the last send.</summary>
internal readonly struct UnsentCreateTag : ITag;
