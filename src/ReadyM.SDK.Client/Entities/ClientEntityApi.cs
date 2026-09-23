using System.Collections.Concurrent;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Exceptions;

namespace ReadyM.SDK.Client.Entities;

internal sealed class ClientEntityApi(
    EntityStore store,
    ILogger<ClientEntityApi> logger,
    // Lazy because this is built early and the mapping policy directory is not.
    Lazy<IMappingPolicyDirectory>? policies = null
) : IEntityApi
{
    private readonly ConcurrentDictionary<Type, IMappingDataPolicy<Entity>?> _policyOf = new();
    private QueryScope _scope = new();

    public bool Write<TComponent, TValue>(
        RawEntity rawEntity,
        ref TComponent component,
        Field<TComponent, TValue> field,
        TValue value,
        WriteKind kind)
        where TComponent : struct, IComponent
    {
        if (!Allows(rawEntity, typeof(TComponent), kind))
            return false;

        switch (kind)
        {
            case WriteKind.Mirror when field.WasSetFromApi(component):
                return false;
            case WriteKind.Override:
                field.SetFromApi(ref component, value, rawEntity.Id);
                break;
            default:
                field.Set(ref component, value);
                break;
        }

        return true;
    }

    public bool Allows(RawEntity rawEntity, Type component, WriteKind kind)
    {
        // No policy means everything is allowed.
        if (PolicyOf(component) is not { } policy)
            return true;

        var entity = store.GetEntityByRawEntity(rawEntity);

        if (kind == WriteKind.Override ? policy.CanSetFromApi(entity) : policy.ShouldGameCopyToEcs(entity))
            return true;

        if (kind == WriteKind.Override)
            logger.LogWarning("Refused an override of {Component} on {Entity}: it is not this client's to set", component.Name, rawEntity.Id);
        else
            logger.LogDebug("Refused a write of {Component} on {Entity}", component.Name, rawEntity.Id);

        return false;
    }

    public bool ShouldApplyToGame(RawEntity rawEntity, Type component)
        => PolicyOf(component) is not { } policy || policy.ShouldEcsCopyToGame(store.GetEntityByRawEntity(rawEntity));

    private IMappingDataPolicy<Entity>? PolicyOf(Type component)
    {
        if (policies is null)
            return null;

        if (_policyOf.TryGetValue(component, out var found))
            return found;

        try
        {
            found = policies.Value.ForData(component);
        }
        catch (ArgumentException)
        {
            // What the directory does for a component no policy covers.
            found = null;
        }

        return _policyOf[component] = found;
    }

    public int ComponentIdOf(Type type) => SchemaTypeUtils.GetStructIndex(type);

    public ComponentRef Locate(RawEntity rawEntity, int componentId)
    {
        var nodes = store.nodes;

        if ((uint)rawEntity.Id >= (uint)nodes.Length)
            throw new InvalidEntityException();

        ref var node = ref nodes[rawEntity.Id];

        if (!node.IsAlive(rawEntity.Revision) || _scope.IsPending(rawEntity))
            throw new InvalidEntityException();

        var heap = node.archetype.heapMap[componentId];

        return heap is null ? default : new ComponentRef(heap.ComponentArray!, node.compIndex);
    }

    public RawEntity Create(ComponentSet components, RawEntity scope)
    {
        var holder = Resolve(scope);
        var entity = store.GetEntityByRawEntity(Create(components));

        entity.AddComponent(new InScopeComponent(holder));

        return entity.RawEntity;
    }

    public RawEntity Create(ComponentSet components)
    {
        _scope.RefuseIfInQuery("Creating an entity");

        return Created(store.GetArchetype(ClientComponents.Resolve(components)).CreateEntity().RawEntity, components);
    }

    /// A component holding a native collection has no memory until this runs.
    private RawEntity Created(RawEntity rawEntity, ComponentSet components)
    {
        if (NativeInitRegistry.Any)
            NativeInitRegistry.InitAll(new EntityHandle(rawEntity, this), components);

        return rawEntity;
    }

    /// Friflo does not update indices unless the component is replaced whole.
    public void ReplaceIndexed<TComponent, TKey>(RawEntity rawEntity, TComponent component)
        where TComponent : struct, IIndexedComponent<TKey>
        => Resolve(rawEntity).AddComponent(component);

    public bool TryFindByIndex<TComponent, TKey>(TKey key, out RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey>
    {
        foreach (var found in store.ComponentIndex<TComponent, TKey>()[key])
        {
            entity = found.RawEntity;
            return true;
        }

        entity = default;
        return false;
    }

    public EntityBuffer CollectInScope(RawEntity scope, ComponentSet components)
    {
        var target = Resolve(scope);
        var wanted = ClientComponents.Resolve(components);
        var links = target.GetIncomingLinks<InScopeComponent>();
        var buffer = EntityBuffer.Rent();

        for (var i = 0; i < links.Count; i++)
        {
            var entity = links.Entities[i];

            if (entity.Archetype.ComponentTypes.HasAll(wanted))
                buffer.Append(entity.RawEntity);
        }

        return buffer;
    }

    public bool HasComponents(RawEntity rawEntity, ComponentSet components)
        => Resolve(rawEntity).Archetype.ComponentTypes.HasAll(ClientComponents.Resolve(components));

    public bool IsAlive(RawEntity rawEntity)
        => !_scope.IsPending(rawEntity) && !store.GetEntityByRawEntity(rawEntity).IsNull;

    public bool Delete(RawEntity rawEntity)
    {
        if (!IsAlive(rawEntity))
            return false;

        if (_scope.InQuery)
            return _scope.Mark(rawEntity);

        DeleteNow(rawEntity);
        return true;
    }

    public void EnterQuery() => _scope.Enter();

    public void LeaveQuery()
    {
        if (!_scope.Leave())
            return;

        foreach (var rawEntity in _scope.Pending)
            DeleteNow(rawEntity);

        _scope.Clear();
    }

    private void DeleteNow(RawEntity rawEntity)
    {
        var entity = store.GetEntityByRawEntity(rawEntity);

        if (!entity.IsNull)
            entity.DeleteEntity();
    }

    private Entity Resolve(RawEntity rawEntity)
    {
        if (_scope.IsPending(rawEntity))
            throw new InvalidEntityException();

        var entity = store.GetEntityByRawEntity(rawEntity);

        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity;
    }
}