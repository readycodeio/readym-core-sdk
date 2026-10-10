using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Marks an unsent create that a client made, so that client is left out of it.</summary>
internal struct CreatedByComponent(PlayerId origin) : IIndexedComponent<PlayerId>
{
    public PlayerId Origin = origin;

    public PlayerId GetIndexedValue()
        => Origin;
}
