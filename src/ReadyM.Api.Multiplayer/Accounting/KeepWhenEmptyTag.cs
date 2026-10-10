using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Marks an area or cell scope entity that stays, with its entities, when nobody is in it any more.</summary>
internal readonly struct KeepWhenEmptyTag : ITag;
