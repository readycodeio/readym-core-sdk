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

/// <summary>The id of the cell's new scope entity, or 0 if the cell already has one or its area has none.</summary>
internal delegate RawEntity CreateCellScopeEntityDelegate(FullCellId cellId);

/// <summary>1 and the cell the entity is scoped to written to <paramref name="cellId"/> if it is in a cell scope, else 0.</summary>
internal unsafe delegate byte TryGetEntityCellDelegate(int entityId, FullCellId* cellId);

/// <summary>1 and the scope holding the entity written to <paramref name="scope"/> if it is in one, else 0.</summary>
/// <remarks>
/// One step up. Recursive search is done by the caller if neccessary.
/// </remarks>
internal unsafe delegate byte TryGetEntityScopeDelegate(RawEntity entity, RawEntity* scope);

/// <summary>1 if the entity is a resident now, 0 if it is not in a cell.</summary>
internal delegate byte MakeResidentDelegate(RawEntity entity);

/// <summary>1 if the entity is no longer a resident, 0 if it is not a resident standing in a cell.</summary>
internal delegate byte EndResidencyDelegate(RawEntity entity);

/// <summary>1 if the entity is a resident, else 0.</summary>
internal delegate byte IsResidentDelegate(RawEntity entity);
