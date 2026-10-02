using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Relay.Server.Sdk.Interop;

// These mirror the server state's methods of the same names one to one. An exception cannot cross the
// interop boundary, so where the server state throws, these answer with a default RawEntity.
//
// Identity rather than a bare id, because that is what the mod-facing Entity is built from: an id on its
// own cannot tell this entity apart from a later one that took its number.

/// <summary>1 and the area's scope entity written to <paramref name="entity"/> if it has one, else 0.</summary>
internal unsafe delegate byte TryGetAreaScopeEntityDelegate(AreaId areaId, RawEntity* entity);

/// <summary>The area's new scope entity, or default if the area already has one.</summary>
internal delegate RawEntity CreateAreaScopeEntityDelegate(AreaId areaId);

/// <summary>1 and the cell's scope entity written to <paramref name="entity"/> if it has one, else 0.</summary>
internal unsafe delegate byte TryGetCellScopeEntityDelegate(FullCellId cellId, RawEntity* entity);

/// <summary>The cell's new scope entity, or default if the cell already has one or its area has none.</summary>
internal delegate RawEntity CreateCellScopeEntityDelegate(FullCellId cellId);
