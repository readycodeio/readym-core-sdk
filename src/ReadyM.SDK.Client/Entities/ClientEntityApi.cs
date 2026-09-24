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

internal sealed class ClientEntityApi : IEntityApi
{
    private readonly EntityStore _store;
    private readonly ILogger<ClientEntityApi> _logger;

    // Lazy because this is built early and the mapping policy directory is not.
    private readonly Lazy<IMappingPolicyDirectory>? _policies;

    private readonly ConcurrentDictionary<Type, IMappingDataPolicy<Entity>?> _policyOf = new();
    private QueryScope _scope = new();

    public ClientEntityApi(
        EntityStore store,
        ILogger<ClientEntityApi> logger,
        Lazy<IMappingPolicyDirectory>? policies = null)
    {
        _store = store;
        _logger = logger;
        _policies = policies;

        store.OnEntityDelete += OnEntityDelete;
    }
    
    private void OnEntityDelete(EntityDelete going)
    {
        if (!DeleteHandlerRegistry.Any)
            return;

        var entity = going.Entity;
        DeleteHandlerRegistry.RunFor(new EntityHandle(entity.RawEntity, this), entity.Archetype.ComponentTypes);
    }

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

    public bool Mirrors<TComponent, TValue>(
        RawEntity rawEntity,
        in TComponent component,
        Field<TComponent, TValue> field)
        where TComponent : struct, IComponent
        => Allows(rawEntity, typeof(TComponent), WriteKind.Mirror) && !field.WasSetFromApi(component);

    public bool Allows(RawEntity rawEntity, Type component, WriteKind kind)
    {
        // No policy means everything is allowed.
        if (PolicyOf(component) is not { } policy)
            return true;

        var entity = _store.GetEntityByRawEntity(rawEntity);

        if (kind == WriteKind.Override ? policy.CanSetFromApi(entity) : policy.ShouldGameCopyToEcs(entity))
            return true;

        if (kind == WriteKind.Override)
            _logger.LogWarning("Refused an override of {Component} on {Entity}: it is not this client's to set", component.Name, rawEntity.Id);
        else
            _logger.LogDebug("Refused a write of {Component} on {Entity}", component.Name, rawEntity.Id);

        return false;
    }

    public bool ShouldApplyToGame(RawEntity rawEntity, Type component)
        => PolicyOf(component) is not { } policy || policy.ShouldEcsCopyToGame(_store.GetEntityByRawEntity(rawEntity));

    private IMappingDataPolicy<Entity>? PolicyOf(Type component)
    {
        if (_policies is null)
            return null;

        if (_policyOf.TryGetValue(component, out var found))
            return found;

        try
        {
            found = _policies.Value.ForData(component);
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
        var nodes = _store.nodes;

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
        var entity = _store.GetEntityByRawEntity(Made(components));

        // Before what the shape asked runs, so a handler finds the entity where it will live.
        entity.AddComponent(new InScopeComponent(holder));

        return Created(entity.RawEntity, components);
    }

    public RawEntity Create(ComponentSet components) => Created(Made(components), components);

    private RawEntity Made(ComponentSet components)
    {
        _scope.RefuseIfInQuery("Creating an entity");

        return _store.GetArchetype(ClientComponents.Resolve(components)).CreateEntity().RawEntity;
    }

    /// A component holding a native collection has no memory until this runs, and what a shape asked
    /// to run follows it, once the entity is whole.
    private RawEntity Created(RawEntity rawEntity, ComponentSet components)
    {
        var handle = new EntityHandle(rawEntity, this);

        if (NativeInitRegistry.Any)
            NativeInitRegistry.InitAll(handle, components);

        if (CreateHandlerRegistry.Any)
            CreateHandlerRegistry.RunAll(handle, components);

        return rawEntity;
    }

    /// Friflo does not update indices unless the component is replaced whole.
    public void ReplaceIndexed<TComponent, TKey>(RawEntity rawEntity, TComponent component)
        where TComponent : struct, IIndexedComponent<TKey>
        => Resolve(rawEntity).AddComponent(component);

    public bool TryFindByIndex<TComponent, TKey>(TKey key, out RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey>
    {
        foreach (var found in _store.ComponentIndex<TComponent, TKey>()[key])
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
        => !_scope.IsPending(rawEntity) && !_store.GetEntityByRawEntity(rawEntity).IsNull;

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

        _scope.BeginDrain();

        foreach (var rawEntity in _scope.Pending)
            DeleteNow(rawEntity);

        _scope.Clear();
    }

    private void DeleteNow(RawEntity rawEntity)
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);

        if (!entity.IsNull)
            entity.DeleteEntity();
    }

    private Entity Resolve(RawEntity rawEntity)
    {
        if (_scope.IsPending(rawEntity))
            throw new InvalidEntityException();

        var entity = _store.GetEntityByRawEntity(rawEntity);

        if (entity.IsNull)
            throw new InvalidEntityException();

        return entity;
    }
}