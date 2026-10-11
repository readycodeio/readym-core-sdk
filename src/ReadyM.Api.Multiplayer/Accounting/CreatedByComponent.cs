using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>Marks an unsent create a client made from a scope it was still sent: that client holds it already.</summary>
internal struct CreatedByComponent(PlayerId origin) : IComponent
{
    public PlayerId Origin = origin;
}
