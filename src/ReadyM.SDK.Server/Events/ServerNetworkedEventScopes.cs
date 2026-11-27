using ReadyM.Api.Idents;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Server.Entities;

namespace ReadyM.SDK.Server.Events;

/// <summary>Finds the areas and cells a networked event names, or that its subject is in.</summary>
internal sealed class ServerNetworkedEventScopes(IEntityApi api)
{
    public bool TryFindArea(FullAreaId id, out Area area) => TryFind(id, out area);

    public bool TryFindCell(FullCellId id, out Cell cell) => TryFind(id, out cell);

    /// <summary>The entity's dimension; the main one where the host keeps none.</summary>
    public DimensionId DimensionOf(EntityHandle entity)
        => (api as ServerEntityApi)?.DimensionOf(entity.RawEntity) ?? DimensionId.Main;

    /// <summary>The area the entity is in, directly or through its cell.</summary>
    public bool TryFindAreaOf(EntityHandle entity, out FullAreaId area)
    {
        var found = ScopeLookup.TryFind<Area>(api, entity, out var scope);
        area = found ? scope.FullAreaId : default;

        return found;
    }

    public bool TryFindCellOf(EntityHandle entity, out FullCellId cell)
    {
        var found = ScopeLookup.TryFind<Cell>(api, entity, out var scope);
        cell = found ? scope.FullCellId : default;

        return found;
    }

    private bool TryFind<TScope, TId>(TId id, out TScope scope)
        where TScope : struct, IArchetypeQueryable, IIndexed<TId>
    {
        if (IndexRegistry.TryFind<TScope, TId>(api, id, out var raw))
        {
            scope = new TScope { Handle = new EntityHandle(raw, api) };
            return true;
        }

        scope = default;
        return false;
    }
}
