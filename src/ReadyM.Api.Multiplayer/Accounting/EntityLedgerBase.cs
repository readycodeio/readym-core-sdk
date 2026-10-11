using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Managers;
using ReadyM.Api.Multiplayer.ECS.Values;

namespace ReadyM.Api.Multiplayer.Accounting;

/// <summary>
/// Records every change to a networked entity that others must hear about, applying it at once, and hands the sending
/// systems what is still owed as entries. What is owed about an entity is a mark on the entity naming its group; an
/// entry is a group or a scope and the players owed it. Once nothing is owed the marks go. ECS thread only.
/// </summary>
internal abstract class EntityLedgerBase : IDisposable
{
    private readonly ComponentIndex<LedgerGroupComponent, int> _groupIndex;
    private readonly Dictionary<LedgerGroup, int> _groupIds = [];
    private readonly Dictionary<int, GroupState> _groups = [];
    private readonly List<Draft> _drafts = [];
    private readonly List<Entity> _scratch = [];
    private Dictionary<EntryKey, int> _entryIds = [];
    private List<LedgerEntry> _pending = [];
    private int _nextEntryId;
    private int _nextGroupId;
    private bool _dirty;
    private bool _sending;

    protected EntityLedgerBase(Store world, NetworkedEntityManager netEntity, ILogger logger)
    {
        World = world;
        NetEntity = netEntity;
        Logger = logger;
        _groupIndex = world.ComponentIndex<LedgerGroupComponent, int>();
        world.OnEntityDelete += OnEntityDelete;
    }

    protected Store World { get; }

    protected NetworkedEntityManager NetEntity { get; }

    protected ILogger Logger { get; }

    /// <summary>What is still owed, each entry the inputs of a query and the players owed what it returns.</summary>
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
            return _pending.Count == 0 && _groups.Count == 0;
        }
    }

    public void Dispose()
        => World.OnEntityDelete -= OnEntityDelete;

    /// <summary>The query an entry's inputs describe.</summary>
    public ArchetypeQuery<MetadataComponent> Query(in LedgerEntry entry)
    {
        switch (entry.Kind)
        {
            case LedgerEntryKind.EnterScope when entry.Scope == default:
                return World.Query<MetadataComponent>(new QueryFilter()
                    .WithoutAnyComponents(ComponentTypes.Get<InScopeComponent>())
                    .WithoutAnyTags(Tags.Get<ScopeEntityTag>()));

            case LedgerEntryKind.EnterScope:
                return World.Query<MetadataComponent>().HasValue<InScopeComponent, Entity>(entry.Scope);

            case LedgerEntryKind.OwnerChange:
                return World.Query<MetadataComponent>(new QueryFilter().AllTags(Tags.Get<OwnerChangedTag>()))
                    .HasValue<LedgerGroupComponent, int>(entry.Group);

            default:
                return World.Query<MetadataComponent>().HasValue<LedgerGroupComponent, int>(entry.Group);
        }
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
        OnSent(_pending[index]);
        _dirty = true;
    }

    /// <summary>The scope entity an entity is in, or a null entity for the global scope.</summary>
    public Entity ScopeOf(Entity entity)
        => entity.TryGetComponent<InScopeComponent>(out var inScope) ? inScope.ScopeEntity : default;

    /// <summary>Whether the player sees the scope; a null scope entity is the global scope.</summary>
    public abstract bool Sees(PlayerId player, Entity scope);

    /// <summary>Adds what is owed now, through <see cref="Owe"/>.</summary>
    protected abstract void Collect();

    /// <summary>Records what sending a pending entry changed.</summary>
    protected abstract void OnSent(in LedgerEntry entry);

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

    /// <summary>Adds an entry to what is owed; the players list becomes the entry's own.</summary>
    protected void Owe(LedgerEntryKind kind, Entity scope, NetworkId scopeNetId, Entity from, PlayerId excludedOrigin,
        List<PlayerId> players, List<NetworkId>? netIds, int group)
        => _drafts.Add(new Draft(new EntryKey(kind, group, scope, from, excludedOrigin, PlayerSet.Of(players)),
            scopeNetId, players, netIds));

    /// <summary>A fresh id for a group or for anything else an entry names.</summary>
    protected int NextId()
        => ++_nextGroupId;

    /// <summary>Every group that has had an entity this round, emptied or not.</summary>
    protected Dictionary<int, GroupState>.ValueCollection Groups => _groups.Values;

    protected GroupState GroupOf(int id)
        => _groups[id];

    /// <summary>The entities of a group; copy before changing any of them.</summary>
    protected Entities EntitiesOf(int group)
        => _groupIndex[group];

    protected bool TryGetGroup(Entity entity, out LedgerGroup group)
    {
        if (entity.TryGetComponent<LedgerGroupComponent>(out var mark))
        {
            group = _groups[mark.Group].Key;
            return true;
        }

        group = default;
        return false;
    }

    /// <summary>Puts the entity in a group, or takes it out of any when nothing is owed about it.</summary>
    protected void SetGroup(Entity entity, in LedgerGroup group)
    {
        if (group.OwesNothing && !entity.Tags.Has<OwnerChangedTag>())
        {
            if (entity.HasComponent<LedgerGroupComponent>())
                entity.RemoveComponent<LedgerGroupComponent>();
            return;
        }

        if (!_groupIds.TryGetValue(group, out var id))
        {
            id = NextId();
            _groupIds.Add(group, id);
            _groups.Add(id, new GroupState(id, group));
        }

        if (!entity.TryGetComponent<LedgerGroupComponent>(out var mark) || mark.Group != id)
            entity.AddComponent(new LedgerGroupComponent(id));
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
        var pending = new List<LedgerEntry>(_drafts.Count);
        foreach (var draft in _drafts)
        {
            if (!_entryIds.TryGetValue(draft.Key, out var id))
                id = ++_nextEntryId;
            ids[draft.Key] = id;

            var netIds = draft.NetIds is null
                ? ReadyM.Api.Helpers.ReadOnlyList<NetworkId>.Empty
                : new ReadyM.Api.Helpers.ReadOnlyList<NetworkId>(draft.NetIds);
            pending.Add(new LedgerEntry(id, draft.Key.Kind, draft.Key.Scope, draft.ScopeNetId, draft.Key.From,
                draft.Key.ExcludedOrigin, new ReadyM.Api.Helpers.ReadOnlyList<PlayerId>(draft.Players), netIds, draft.Key.Group));
        }

        _drafts.Clear();
        _entryIds = ids;
        _pending = pending;

        if (pending.Count == 0)
            Commit();
    }

    /// <summary>Nothing is owed: every mark goes, and with it every group.</summary>
    private void Commit()
    {
        _scratch.Clear();
        foreach (var id in _groups.Keys)
        {
            foreach (var entity in _groupIndex[id])
                _scratch.Add(entity);
        }

        foreach (var entity in _scratch)
        {
            entity.RemoveComponent<LedgerGroupComponent>();
            if (entity.Tags.Has<OwnerChangedTag>())
                entity.RemoveTag<OwnerChangedTag>();
        }

        _scratch.Clear();
        _groups.Clear();
        _groupIds.Clear();
        _entryIds.Clear();
        _sending = false;
        OnCommit();
    }

    /// <summary>One group, and which of its players were sent what this round.</summary>
    protected sealed class GroupState(int id, LedgerGroup key)
    {
        public int Id { get; } = id;

        public LedgerGroup Key { get; } = key;

        /// <summary>Players sent a create, scope change or delete of the group: each is sent one of them at most.</summary>
        public HashSet<PlayerId> Main { get; } = [];

        /// <summary>Players sent the group's ownership change.</summary>
        public HashSet<PlayerId> Owner { get; } = [];
    }

    private readonly struct EntryKey(LedgerEntryKind kind, int group, Entity scope, Entity from, PlayerId excludedOrigin, PlayerSet players)
        : IEquatable<EntryKey>
    {
        public LedgerEntryKind Kind { get; } = kind;

        public int Group { get; } = group;

        public Entity Scope { get; } = scope;

        public Entity From { get; } = from;

        public PlayerId ExcludedOrigin { get; } = excludedOrigin;

        public PlayerSet Players { get; } = players;

        public bool Equals(EntryKey other)
            => Kind == other.Kind && Group == other.Group && Scope == other.Scope && From == other.From
               && ExcludedOrigin == other.ExcludedOrigin && Players.Equals(other.Players);

        public override bool Equals(object? obj)
            => obj is EntryKey other && Equals(other);

        public override int GetHashCode()
        {
            var hash = (int)Kind;
            hash = hash * 31 + Group;
            hash = hash * 31 + Scope.GetHashCode();
            hash = hash * 31 + From.GetHashCode();
            hash = hash * 31 + ExcludedOrigin.GetHashCode();
            return hash * 31 + Players.GetHashCode();
        }
    }

    private readonly struct Draft(EntryKey key, NetworkId scopeNetId, List<PlayerId> players, List<NetworkId>? netIds)
    {
        public EntryKey Key { get; } = key;

        public NetworkId ScopeNetId { get; } = scopeNetId;

        public List<PlayerId> Players { get; } = players;

        public List<NetworkId>? NetIds { get; } = netIds;
    }
}
