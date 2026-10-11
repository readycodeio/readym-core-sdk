using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;
using EntitySelection = (Friflo.Engine.ECS.Entity Scope, Friflo.Engine.ECS.Entity From, bool Unsent, ReadyM.Api.Idents.PlayerId Creator, Friflo.Engine.ECS.Entity Player);

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>
/// Records every change to a networked entity that others must hear about, applying it at once, and hands the sending
/// systems what is still owed as entries. What is owed about an entity is a set of marks on the entity: unsent, moved
/// from the scope it was last sent in, made by a client, owner changed. An entry is the inputs that pick entities by
/// those marks, and the players owed them. Once nothing is owed the marks go. ECS thread only.
/// </summary>
internal abstract class EntityLedgerBase : IDisposable
{
    private readonly ComponentIndex<LastSentInComponent, RawEntity> _lastSentIn;
    private readonly ArchetypeQuery<MetadataComponent> _unsent;
    private readonly ArchetypeQuery<MetadataComponent> _moved;
    private readonly ArchetypeQuery<MetadataComponent> _ownerChanged;
    private readonly ArchetypeQuery<MetadataComponent> _createdBy;
    private readonly ArchetypeQuery<MetadataComponent> _globalContent;
    private readonly EntityList _entities = new();
    private readonly EntityList _selected = new();
    private readonly List<Draft> _drafts = [];
    private readonly List<Entity> _scratch = [];
    private Dictionary<EntryKey, int> _entryIds = [];
    private List<LedgerEntry> _pending = [];
    private List<List<int>> _sources = [];
    private int _nextEntryId;
    private bool _dirty;
    private bool _sending;
    private bool _marked;

    protected EntityLedgerBase(Store world, NetworkedEntityManager netEntity, ILogger logger)
    {
        World = world;
        NetEntity = netEntity;
        Logger = logger;
        _lastSentIn = world.ComponentIndex<LastSentInComponent, RawEntity>();
        _unsent = world.Query<MetadataComponent>(new QueryFilter().AllTags(Tags.Get<LocallyCreatedEntityTag>()));
        _moved = world.Query<MetadataComponent>(new QueryFilter().AllComponents(ComponentTypes.Get<LastSentInComponent>()));
        _ownerChanged = world.Query<MetadataComponent>(new QueryFilter().AllTags(Tags.Get<OwnerChangedTag>()));
        _createdBy = world.Query<MetadataComponent>(new QueryFilter().AllComponents(ComponentTypes.Get<CreatedByComponent>()));
        _globalContent = world.Query<MetadataComponent>(new QueryFilter()
            .WithoutAnyComponents(ComponentTypes.Get<InScopeComponent>())
            .WithoutAnyTags(Tags.Get<ScopeEntityTag>()));
        world.OnEntityDelete += OnEntityDelete;
    }

    protected Store World { get; }

    protected NetworkedEntityManager NetEntity { get; }

    protected ILogger Logger { get; }

    /// <summary>What is still owed, each entry the inputs that pick entities and the players owed them.</summary>
    public ReadyM.Api.Helpers.ReadOnlyList<LedgerEntry> Pending
    {
        get
        {
            Refresh();
            return new ReadyM.Api.Helpers.ReadOnlyList<LedgerEntry>(_pending);
        }
    }

    /// <summary>Whether nothing is owed: no entries, and no entity carries a mark.</summary>
    public bool IsSettled
    {
        get
        {
            Refresh();
            return _pending.Count == 0 && !_marked;
        }
    }

    public void Dispose()
        => World.OnEntityDelete -= OnEntityDelete;

    /// <summary>
    /// The entities an entry is about: every entity in the scope for a snapshot, the marked entities its inputs pick
    /// for anything else, none for a leave or a delete. The list is reused: it holds until the next call.
    /// </summary>
    public EntityList Entities(in LedgerEntry entry)
    {
        _entities.Clear();
        switch (entry.Kind)
        {
            case LedgerEntryKind.EnterScope when entry.Scope == default:
                foreach (var entity in _globalContent.Entities)
                    Add(_entities, entity);
                break;

            case LedgerEntryKind.EnterScope:
                foreach (var link in entry.Scope.GetIncomingLinks<InScopeComponent>())
                    Add(_entities, link.Entity);
                break;

            case LedgerEntryKind.Create:
            case LedgerEntryKind.ScopeChange:
            case LedgerEntryKind.OwnerChange:
                AddSelected(_entities, (entry.Scope, entry.From, entry.Unsent, entry.Creator, entry.Player), entry.Kind == LedgerEntryKind.OwnerChange);
                break;
        }

        return _entities;
    }

    /// <summary>Records that the entry's entities were sent to the entry's players; throws for an entry not pending.</summary>
    public void Sent(in LedgerEntry entry)
    {
        Refresh();
        var index = -1;
        for (var i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].Id == entry.Id)
                index = i;
        }

        if (index < 0)
            throw new InvalidOperationException($"Ledger entry {entry.Id} ({entry.Kind}) is not pending: it was sent already, or is out of date");

        _sending = true;
        foreach (var source in _sources[index])
            OnSent(_pending[index], source);
        _dirty = true;
    }

    /// <summary>The scope entity an entity is in, or a null entity for the global scope.</summary>
    public Entity ScopeOf(Entity entity)
        => entity.TryGetComponent<InScopeComponent>(out var inScope) ? inScope.ScopeEntity : default;

    /// <summary>Whether the player sees the scope; a null scope entity is the global scope.</summary>
    public abstract bool Sees(PlayerId player, Entity scope);

    /// <summary>Adds what is owed now, through <see cref="Owe"/>.</summary>
    protected abstract void Collect();

    /// <summary>
    /// Records what sending a pending entry changed, once for each source <see cref="Owe"/> was given for it: drafts
    /// that pick the same entities for the same players are one entry, their net ids together.
    /// </summary>
    protected abstract void OnSent(in LedgerEntry entry, int source);

    /// <summary>A networked entity is being deleted, by anyone; it can still be read.</summary>
    protected abstract void OnDeleted(Entity entity);

    /// <summary>Nothing is owed any more: whatever was kept for this round can go.</summary>
    protected virtual void OnCommit()
    {
    }

    /// <summary>Called before every change: entries already sent would miss a change made while others are pending.</summary>
    protected void Changing()
    {
        if (_sending)
            Refresh();
        if (_sending)
            throw new InvalidOperationException("The entity ledger takes changes only before its entries are sent, or once all are");

        _dirty = true;
    }

    /// <summary>
    /// Adds an entry to what is owed; the players list becomes the entry's own. The source, the side's own index of what
    /// the entry stands for in this rebuild, comes back to <see cref="OnSent"/>.
    /// </summary>
    protected void Owe(LedgerEntryKind kind, in EntitySelection selection, NetworkId scopeNetId, PlayerId excludedOrigin,
        List<PlayerId> players, List<NetworkId>? netIds, int source = -1)
        => _drafts.Add(new Draft(kind, selection, scopeNetId, excludedOrigin, players, netIds, source));

    /// <summary>What an entity's marks say about it: where it is, where it was last sent, whether unsent and who holds it.</summary>
    protected EntitySelection SelectionOf(Entity entity)
    {
        var scope = ScopeOf(entity);
        var from = entity.TryGetComponent<LastSentInComponent>(out var lastSentIn) ? lastSentIn.Scope : scope;
        var creator = entity.TryGetComponent<CreatedByComponent>(out var createdBy) ? createdBy.Origin : PlayerId.Server;
        return (scope, from, entity.Tags.Has<LocallyCreatedEntityTag>(), creator, PlayerOf(entity));
    }

    /// <summary>The entities a selection picks, in a list of the ledger's own that holds until the next call.</summary>
    protected EntityList Selected(in EntitySelection selection)
    {
        _selected.Clear();
        AddSelected(_selected, selection, false);
        return _selected;
    }

    /// <summary>The entity itself when it must be picked on its own; a null entity otherwise.</summary>
    protected virtual Entity PlayerOf(Entity entity)
        => default;

    /// <summary>Every entity carrying a mark, each once, for <see cref="Collect"/>.</summary>
    protected List<Entity> MarkedEntities()
    {
        _scratch.Clear();
        foreach (var entity in _unsent.Entities)
            _scratch.Add(entity);
        foreach (var entity in _moved.Entities)
        {
            if (!entity.Tags.Has<LocallyCreatedEntityTag>())
                _scratch.Add(entity);
        }

        foreach (var entity in _ownerChanged.Entities)
        {
            if (!entity.Tags.Has<LocallyCreatedEntityTag>() && !entity.HasComponent<LastSentInComponent>())
                _scratch.Add(entity);
        }

        return _scratch;
    }

    protected void MarkUnsent(Entity entity, PlayerId holder)
    {
        if (!entity.Tags.Has<LocallyCreatedEntityTag>())
            entity.AddTag<LocallyCreatedEntityTag>();
        if (holder != PlayerId.Server)
            entity.AddComponent(new CreatedByComponent(holder));
        _marked = true;
    }

    protected void MarkOwnerChanged(Entity entity)
    {
        if (!entity.Tags.Has<OwnerChangedTag>())
            entity.AddTag<OwnerChangedTag>();
        _marked = true;
    }

    /// <summary>Puts an entity in another scope, remembering where it was last sent if anyone holds it as it was.</summary>
    protected void MoveMarked(Entity entity, Entity scope)
    {
        var heldAsSent = !entity.Tags.Has<LocallyCreatedEntityTag>() || entity.HasComponent<CreatedByComponent>();
        if (heldAsSent && !entity.HasComponent<LastSentInComponent>())
        {
            entity.AddComponent(new LastSentInComponent(ScopeOf(entity)));
            _marked = true;
        }

        PutInScope(entity, scope);
        if (entity.TryGetComponent<LastSentInComponent>(out var lastSentIn) && lastSentIn.Scope == scope)
            entity.RemoveComponent<LastSentInComponent>();
    }

    /// <summary>Deletes every entity in a scope; the scope entity itself stays.</summary>
    protected static void DeleteContents(Entity scope)
    {
        var contents = new List<Entity>();
        foreach (var link in scope.GetIncomingLinks<InScopeComponent>())
            contents.Add(link.Entity);

        foreach (var entity in contents)
        {
            if (!entity.IsNull)
                entity.DeleteEntity();
        }
    }

    /// <summary>Puts an entity in a scope, or in the global scope for a null scope entity.</summary>
    protected static void PutInScope(Entity entity, Entity scope)
    {
        if (scope == default)
        {
            if (entity.HasComponent<InScopeComponent>())
                entity.RemoveComponent<InScopeComponent>();
        }
        else if (!entity.TryGetComponent<InScopeComponent>(out var inScope) || inScope.ScopeEntity != scope)
        {
            entity.AddComponent(new InScopeComponent(scope));
        }
    }

    protected static NetworkId NetIdOf(Entity entity)
        => entity == default || entity.IsNull ? default : entity.GetComponent<MetadataComponent>().NetId;

    /// <summary>Adds the entities a selection picks, looking only at the smallest set of marked entities it can be in.</summary>
    private void AddSelected(EntityList list, in EntitySelection selection, bool ownerChangedOnly)
    {
        if (selection.Player != default)
        {
            if (!selection.Player.IsNull)
                AddIfSelected(list, selection.Player, selection, ownerChangedOnly);
        }
        else if (selection.Unsent)
        {
            foreach (var entity in _unsent.Entities)
                AddIfSelected(list, entity, selection, ownerChangedOnly);
        }
        else if (selection.From != selection.Scope)
        {
            foreach (var entity in _lastSentIn[selection.From.RawEntity])
                AddIfSelected(list, entity, selection, ownerChangedOnly);
        }
        else
        {
            foreach (var entity in _ownerChanged.Entities)
                AddIfSelected(list, entity, selection, ownerChangedOnly);
        }
    }

    private void AddIfSelected(EntityList list, Entity entity, in EntitySelection selection, bool ownerChangedOnly)
    {
        if (SelectionOf(entity) == selection && (!ownerChangedOnly || entity.Tags.Has<OwnerChangedTag>()))
            Add(list, entity);
    }

    private static void Add(EntityList list, Entity entity)
    {
        if (list.Count == 0)
            list.SetStore(entity.Store);
        list.Add(entity);
    }

    private void OnEntityDelete(EntityDelete evt)
    {
        if (!evt.Entity.HasComponent<MetadataComponent>())
            return;

        Changing();
        OnDeleted(evt.Entity);
    }

    /// <summary>Rebuilds what is owed when anything changed, keeping the id of every entry that is still the same.</summary>
    private void Refresh()
    {
        if (!_dirty)
            return;

        _dirty = false;
        _drafts.Clear();
        Collect();

        var ids = new Dictionary<EntryKey, int>();
        var positions = new Dictionary<EntryKey, int>();
        var pending = new List<LedgerEntry>(_drafts.Count);
        var sources = new List<List<int>>(_drafts.Count);
        var netIdLists = new List<List<NetworkId>?>(_drafts.Count);
        foreach (var draft in _drafts)
        {
            var key = new EntryKey(draft.Kind, draft.Selection, draft.ExcludedOrigin, PlayerSet.Of(draft.Players));
            if (positions.TryGetValue(key, out var at))
            {
                // Two deletes for the same players under the same scope go as one.
                netIdLists[at]!.AddRange(draft.NetIds ?? []);
                sources[at].Add(draft.Source);
                continue;
            }

            if (!_entryIds.TryGetValue(key, out var id))
                id = ++_nextEntryId;
            ids[key] = id;
            positions[key] = pending.Count;

            var s = draft.Selection;
            var netIds = draft.NetIds is null ? null : new List<NetworkId>(draft.NetIds);
            pending.Add(new LedgerEntry(id, draft.Kind, s.Scope, draft.ScopeNetId, s.From, s.Unsent, s.Creator, s.Player,
                draft.ExcludedOrigin, new ReadyM.Api.Helpers.ReadOnlyList<PlayerId>(draft.Players),
                netIds is null ? ReadyM.Api.Helpers.ReadOnlyList<NetworkId>.Empty : new ReadyM.Api.Helpers.ReadOnlyList<NetworkId>(netIds)));
            sources.Add([draft.Source]);
            netIdLists.Add(netIds);
        }

        _drafts.Clear();
        _entryIds = ids;
        _pending = pending;
        _sources = sources;

        if (pending.Count == 0)
            Commit();
    }

    /// <summary>Nothing is owed: every mark goes.</summary>
    private void Commit()
    {
        if (_marked)
        {
            _scratch.Clear();
            foreach (var entity in _unsent.Entities)
                _scratch.Add(entity);
            foreach (var entity in _moved.Entities)
                _scratch.Add(entity);
            foreach (var entity in _ownerChanged.Entities)
                _scratch.Add(entity);
            foreach (var entity in _createdBy.Entities)
                _scratch.Add(entity);

            foreach (var entity in _scratch)
            {
                if (entity.Tags.Has<LocallyCreatedEntityTag>())
                    entity.RemoveTag<LocallyCreatedEntityTag>();
                if (entity.Tags.Has<OwnerChangedTag>())
                    entity.RemoveTag<OwnerChangedTag>();
                if (entity.HasComponent<LastSentInComponent>())
                    entity.RemoveComponent<LastSentInComponent>();
                if (entity.HasComponent<CreatedByComponent>())
                    entity.RemoveComponent<CreatedByComponent>();
            }

            _scratch.Clear();
            _marked = false;
        }

        _entryIds.Clear();
        _sending = false;
        OnCommit();
    }

    private readonly struct EntryKey(LedgerEntryKind kind, EntitySelection selection, PlayerId excludedOrigin, PlayerSet players)
        : IEquatable<EntryKey>
    {
        private readonly LedgerEntryKind _kind = kind;
        private readonly EntitySelection _selection = selection;
        private readonly PlayerId _excludedOrigin = excludedOrigin;
        private readonly PlayerSet _players = players;

        public bool Equals(EntryKey other)
            => _kind == other._kind && _selection == other._selection && _excludedOrigin == other._excludedOrigin
               && _players.Equals(other._players);

        public override bool Equals(object? obj)
            => obj is EntryKey other && Equals(other);

        public override int GetHashCode()
            => ((int)_kind * 31 + _selection.GetHashCode()) * 31 + _players.GetHashCode();
    }

    private readonly struct Draft(LedgerEntryKind kind, EntitySelection selection, NetworkId scopeNetId, PlayerId excludedOrigin,
        List<PlayerId> players, List<NetworkId>? netIds, int source)
    {
        public int Source { get; } = source;

        public LedgerEntryKind Kind { get; } = kind;

        public EntitySelection Selection { get; } = selection;

        public NetworkId ScopeNetId { get; } = scopeNetId;

        public PlayerId ExcludedOrigin { get; } = excludedOrigin;

        public List<PlayerId> Players { get; } = players;

        public List<NetworkId>? NetIds { get; } = netIds;
    }
}
