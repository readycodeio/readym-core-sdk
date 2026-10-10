using Friflo.Engine.ECS;
using Friflo.Json.Fliox;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Marks an entity that moved since the last send, linking the scope entity it was last sent in.</summary>
internal struct MovedFromComponent(Entity from) : ILinkComponent
{
    [Ignore]
    public Entity From = from;

    public Entity GetIndexedValue()
        => From;
}
