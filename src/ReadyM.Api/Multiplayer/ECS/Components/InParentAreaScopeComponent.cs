using Friflo.Engine.ECS;
using Friflo.Json.Fliox;

namespace ReadyM.Api.Multiplayer.ECS.Components;

/// <summary>
/// A link component placed on a cell scope entity that references the scope entity of its parent area.
/// This is a separate component from <see cref="CellScopeComponent"/> because a Friflo component can only carry a
/// single index, and <see cref="CellScopeComponent"/> already indexes cells by <see cref="CellScopeComponent.FullCellId"/>.
/// This link enables querying all cells belonging to a given area in O(1) time, given the area's scope entity.
/// </summary>
internal struct InParentAreaScopeComponent(Entity parentAreaScopeEntity) : ILinkComponent
{
    [Ignore]
    public Entity ParentAreaScopeEntity = parentAreaScopeEntity;

    public Entity GetIndexedValue() => ParentAreaScopeEntity;
}
