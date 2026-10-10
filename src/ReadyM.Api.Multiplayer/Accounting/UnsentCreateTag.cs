using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Marks an entity created since the last send: it is owed as a create to everyone who sees its scope.</summary>
internal readonly struct UnsentCreateTag : ITag;
