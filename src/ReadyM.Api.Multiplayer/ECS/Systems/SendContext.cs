using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

/// <summary>The scope a send pass covers: global, or a player's, an area's or a cell's scope entity.</summary>
internal readonly struct SendContext
{
    private readonly SendScopeKind _kind;

    private SendContext(SendScopeKind kind, PlayerId playerId, Entity? scopeEntity)
    {
        _kind = kind;
        PlayerId = playerId;
        ScopeEntity = scopeEntity;
    }

    /// <summary>The player whose scope this is; only meaningful when <see cref="IsPlayer"/>.</summary>
    public readonly PlayerId PlayerId;

    /// <summary>The scope entity, or null for the global scope.</summary>
    public readonly Entity? ScopeEntity;

    public bool IsArea => _kind == SendScopeKind.Area;
    public bool IsPlayer => _kind == SendScopeKind.Player;
    public bool IsCell => _kind == SendScopeKind.Cell;
    public bool IsGlobal => _kind == SendScopeKind.Global;

    /// <summary>The area's id, read from its scope entity. Only valid when <see cref="IsArea"/>.</summary>
    public AreaId AreaId
        => ScopeEntity!.Value.GetComponent<AreaScopeComponent>().AreaId;

    /// <summary>The cell's id, read from its scope entity. Only valid when <see cref="IsCell"/>.</summary>
    public FullCellId CellId
        => ScopeEntity!.Value.GetComponent<CellScopeComponent>().FullCellId;

    /// <param name="areaEntity">The area's scope entity, which holds its <see cref="AreaScopeComponent"/>.</param>
    public static SendContext FromArea(Entity areaEntity)
        => new(SendScopeKind.Area, default, areaEntity);

    public static SendContext FromPlayer(PlayerId playerId, Entity scopeEntity)
        => new(SendScopeKind.Player, playerId, scopeEntity);

    /// <param name="cellEntity">The cell's scope entity, which holds its <see cref="CellScopeComponent"/>.</param>
    public static SendContext FromCell(Entity cellEntity)
        => new(SendScopeKind.Cell, default, cellEntity);

    public static SendContext Global
        => default;

    private enum SendScopeKind : byte
    {
        Global,
        Player,
        Area,
        Cell,
    }
}
