using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

internal readonly struct SendContext(AreaId? areaId, PlayerId? playerId, FullCellId? cellId, Entity? scopeEntity)
{
    public readonly AreaId? AreaId = areaId;
    public readonly PlayerId? PlayerId = playerId;
    public readonly FullCellId? CellId = cellId;
    public readonly Entity? ScopeEntity = scopeEntity;

    public bool IsArea => AreaId != null;
    public bool IsPlayer => PlayerId != null;
    public bool IsCell => CellId != null;
    public bool IsGlobal => AreaId == null && PlayerId == null && CellId == null;

    public static SendContext FromArea(AreaId areaId, Entity scopeEntity)
        => new SendContext(areaId, null, null, scopeEntity);

    public static SendContext FromPlayer(PlayerId playerId, Entity scopeEntity)
        => new SendContext(null, playerId, null, scopeEntity);

    public static SendContext FromCell(FullCellId cellId, Entity scopeEntity)
        => new SendContext(null, null, cellId, scopeEntity);

    public static SendContext Global
        => new SendContext(null, null, null, null);
}