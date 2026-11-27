using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Values;

namespace ReadyM.Api.ECS.Components;

/// <summary>
/// Carries an entity's <see cref="PersistentId"/>. An archetype is persisted exactly when it declares this component.
/// </summary>
/// <remarks>
/// As with any indexed component, the value must be set through Add/SetComponent or the guid-to-entity index is left stale.
/// </remarks>
internal struct PersistentIdComponent(PersistentId id) : IIndexedComponent<PersistentId>
{
    public PersistentId Id = id;

    public PersistentId GetIndexedValue() => Id;
}
