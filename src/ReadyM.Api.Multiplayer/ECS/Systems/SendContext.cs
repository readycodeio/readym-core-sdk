using Friflo.Engine.ECS;
using ReadyM.Api.Idents;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public readonly struct SendContext(AreaId? areaId, PlayerId? playerId, Entity? scopeEntity)
{
    public readonly AreaId? AreaId = areaId;
    public readonly PlayerId? PlayerId = playerId;
    public readonly Entity? ScopeEntity = scopeEntity;

    public bool IsArea => AreaId != null;
    public bool IsPlayer => PlayerId != null;
    public bool IsGlobal => AreaId == null && PlayerId == null;
        
    public static SendContext FromArea(AreaId areaId, Entity scopeEntity)
        => new SendContext(areaId, null, scopeEntity);
        
    public static SendContext FromPlayer(PlayerId playerId, Entity scopeEntity)
        => new SendContext(null, playerId, scopeEntity);

    public static SendContext Global
        => new SendContext(null, null, null);
}