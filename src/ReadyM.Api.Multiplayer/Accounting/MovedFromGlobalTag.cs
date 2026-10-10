using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Marks an entity last sent as global that has since moved into a scope.</summary>
internal readonly struct MovedFromGlobalTag : ITag;
