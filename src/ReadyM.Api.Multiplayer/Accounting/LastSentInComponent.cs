using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>
/// Marks an entity that moved since it was last sent, with the scope it was last sent in, a null entity for the global
/// scope. Indexed by value, not as a link, so it outlives a scope that is deleted in the same frame.
/// </summary>
internal struct LastSentInComponent(Entity scope) : IIndexedComponent<RawEntity>
{
    public Entity Scope = scope;

    public RawEntity GetIndexedValue()
        => Scope.RawEntity;
}
