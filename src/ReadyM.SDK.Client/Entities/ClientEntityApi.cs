using System.Collections.Concurrent;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Archetypes;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Events;
using ReadyM.SDK.Exceptions;

namespace ReadyM.SDK.Client.Entities;

internal sealed class ClientEntityApi : IEntityApi
{
    private readonly EntityStore _store;
    private readonly ILogger<ClientEntityApi> _logger;

    // Lazy because this is built early and the mapping policy directory is not.
    private readonly Lazy<IMappingPolicyDirectory>? _policies;

    /// <summary>The world an archetype is registered with.</summary>
    private readonly Store? _world;

    /// <summary>Null in a game with no networking, which is every local-only session and most tests.</summary>
    private readonly INetworkedEntityManager? _networked;

    /// <summary>Null in a world that does not predict networked events.</summary>
    private readonly GuessedValueRegistry? _guessedValues;

    /// <summary>One archetype per set, worked out on first use the way the server's half does it.</summary>
    private readonly ConcurrentDictionary<ComponentSet, ArchetypeId> _archetypes = new();

    /// <summary>The tags a shape declared, resolved against this world's schema once per set.</summary>
    private readonly ConcurrentDictionary<ComponentSet, Tags> _tags = new();

    private readonly ConcurrentDictionary<Type, IMappingDataPolicy<Entity>?> _policyOf = new();
    private QueryScope _scope = new();

    public ClientEntityApi(
        EntityStore store,
        ILogger<ClientEntityApi> logger,
        Lazy<IMappingPolicyDirectory>? policies = null,
        Store? world = null,
        INetworkedEntityManager? networked = null,
        GuessedValueRegistry? guessedValues = null)
    {
        _store = store;
        _logger = logger;
        _policies = policies;
        _world = world;
        _networked = networked;
        _guessedValues = guessedValues;

        store.OnEntityDelete += OnEntityDelete;
    }

    private void OnEntityDelete(EntityDelete going)
    {
        if (!DeleteHandlerRegistry.Any)
            return;

        DeleteHandlerRegistry.RunAll(new EntityHandle(going.Entity.RawEntity, this));
    }

    public void Predict<TComponent, TValue>(
        RawEntity rawEntity, Field<TComponent, TValue> field, TValue value, string member)
        where TComponent : struct, INetworkedComponent
    {
        if (_guessedValues is not { } guessedValues)
            throw new InvalidOperationException($"'{member}' was predicted in a world that does not predict.");

        guessedValues.Guess(new EntityHandle(rawEntity, this), field, value, member);
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

    /// <param name="owner">Ignored. A client only ever makes entities of its own.</param>
    public RawEntity Create(ComponentSet components, RawEntity? scope = null, PlayerId? owner = null)
    {
        // Ahead of anything being made, so a scope that is already gone takes nothing with it.
        var holder = scope is { } asked ? Resolve(asked) : (Entity?)null;

        if (Networked(components) is { } manager)
            return Created(manager.CreateNetworkedEntity(Archetype(components), holder).Entity.RawEntity, components);

        var entity = Made(components);

        // Before what the shape asked runs, so a handler finds the entity where it will live.
        if (holder is { } placed)
            _store.GetEntityByRawEntity(entity).AddComponent(new InScopeComponent(placed));

        return Created(entity, components);
    }

    private INetworkedEntityManager? Networked(ComponentSet components)
    {
        if (!components.Replicated)
            return null;

        if (_networked is null || _world is null)
        {
            // A shape that sends, in a session with nothing to send it over. Local is the only
            // thing left to do, and silence would look like the network dropping it.
            _logger.LogWarning(
                "{Components} is declared replicated, but this game registered no networked entity manager, "
                + "so the entity is local and no other side will hear about it.", components);

            return null;
        }

        _scope.RefuseIfInQuery("Creating an entity");
        return _networked;
    }

    private ArchetypeId Archetype(ComponentSet components)
        => _archetypes.GetOrAdd(components, Bound);

    /// <summary>
    /// Settles the archetype of every shape that declared tags, before anything creates an entity.
    /// </summary>
    internal int ApplyDeclaredTags()
    {
        var applied = 0;

        foreach (var shape in ArchetypeRegistry.TaggedShapes())
        {
            if (Activator.CreateInstance(shape) is not IArchetypeQueryable queryable
                || ArchetypeBindings.Of(shape) is not { } id)
                continue;

            var components = ArchetypeRegistry.SetFor(shape, queryable.Components);

            if (TagsOf(components) is { Count: > 0 } tags)
                _world?.ModifyArchetype(id, builder => builder.AddTags(tags));

            applied++;
        }

        return applied;
    }
    
    private Tags TagsOf(ComponentSet components) => _tags.GetOrAdd(components, Declared);

    private static Tags Declared(ComponentSet components)
    {
        var tags = new Tags();

        if (ArchetypeRegistry.ShapeOf(components) is not { } shape)
            return tags;

        var schema = EntityStore.GetEntitySchema();

        foreach (var declared in ArchetypeRegistry.TagsFor(shape))
        {
            if (!schema.TagTypeByType.TryGetValue(declared, out var tagType))
                throw new InvalidOperationException(
                    $"{shape.FullName} asks for the tag {declared.FullName}, which this game's ECS does "
                    + "not know. A tag reaches the schema by being registered with the rest of them "
                    + "before the mods load.");

            tags.Add(new Tags(tagType));
        }

        return tags;
    }

    /// <summary>The archetype this game registered for the shape. Only reached for one that replicates.</summary>
    private ArchetypeId Bound(ComponentSet components)
    {
        var shape = ArchetypeRegistry.ShapeOf(components);

        if (shape is not null && ArchetypeBindings.Of(shape) is { } id)
        {
            if (TagsOf(components) is { Count: > 0 } tags)
                _world?.ModifyArchetype(id, builder => builder.AddTags(tags));

            return id;
        }

        throw new InvalidOperationException(
            $"{shape?.FullName ?? components.ToString()} replicates, but no archetype was registered "
            + "for it. ApplyArchetypeExtensions registers one for every replicated shape loaded by then.");
    }

    private static ArchetypeBuilder Builder(ComponentSet components)
    {
        var builder = new ArchetypeBuilder();

        foreach (var component in components.Types)
            builder.Add(component);

        return builder;
    }

    private RawEntity Made(ComponentSet components)
    {
        _scope.RefuseIfInQuery("Creating an entity");

        return _store.GetArchetype(ClientComponents.Resolve(components), TagsOf(components))
            .CreateEntity()
            .RawEntity;
    }

    /// <summary>
    /// A component holding a native collection has no memory until this runs, and what a shape asked
    /// to run follows it, once the entity is whole.
    /// </summary>
    private RawEntity Created(RawEntity rawEntity, ComponentSet components)
    {
        var handle = new EntityHandle(rawEntity, this);

        if (NativeInitRegistry.Any)
            NativeInitRegistry.InitAll(handle, components);

        if (CreateHandlerRegistry.Any)
            CreateHandlerRegistry.RunAll(handle);

        return rawEntity;
    }

    /// <summary>Friflo does not update indices unless the component is replaced whole.</summary>
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

    public bool TryGetScope(RawEntity entity, out RawEntity scope)
    {
        var target = Resolve(entity);

        if (target.TryGetComponent<InScopeComponent>(out var inScope))
        {
            scope = inScope.ScopeEntity.RawEntity;
            return true;
        }

        // A cell is not in its area's scope, it is under it, so the chain only continues through this.
        if (target.TryGetComponent<InParentAreaScopeComponent>(out var inArea))
        {
            scope = inArea.ParentAreaScopeEntity.RawEntity;
            return true;
        }

        scope = default;
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

    /// <summary>
    /// A networked entity goes through the networked half: the other side is holding a copy and
    /// nothing else would tell it to let go.
    /// </summary>
    private void DeleteNow(RawEntity rawEntity)
    {
        var entity = _store.GetEntityByRawEntity(rawEntity);

        if (entity.IsNull)
            return;

        if (_networked is not null && entity.HasComponent<MetadataComponent>())
        {
            _networked.TryDeleteEntity(entity.Id);
            return;
        }

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